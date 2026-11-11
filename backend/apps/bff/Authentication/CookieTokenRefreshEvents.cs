using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Nodra.Bff.Observability;

namespace Nodra.Bff.Authentication;

/// <summary>
/// Valida a sessão do cookie e renova tokens próximos da expiração.
/// </summary>
public sealed class CookieTokenRefreshEvents(
    IAccessTokenRefreshCoordinator refreshCoordinator,
    TimeProvider timeProvider,
    BffTelemetry telemetry,
    ILogger<CookieTokenRefreshEvents> logger) : CookieAuthenticationEvents
{
    private static readonly TimeSpan s_refreshWindow = TimeSpan.FromMinutes(1);
    private static readonly Action<ILogger, Exception?> s_logRefreshFailure = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(1, "TokenRefreshFailure"),
        "A renovação da sessão no Keycloak falhou.");

    /// <summary>
    /// Verifica a validade dos tokens da sessão e atualiza o principal quando necessário.
    /// </summary>
    /// <param name="context">Contexto da validação do principal autenticado.</param>
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var startedAt = timeProvider.GetTimestamp();
        var expiresAtValue = context.Properties.GetTokenValue("expires_at");
        var hasExpiration = DateTimeOffset.TryParse(
                expiresAtValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
            out var expiresAt);
        if (hasExpiration && expiresAt > timeProvider.GetUtcNow().Add(s_refreshWindow))
        {
            return;
        }

        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            RecordRefresh(startedAt, TokenRefreshOutcome.RefreshTokenMissing);
            await RejectSessionAsync(context, "A sessão não possui refresh token.");
            return;
        }

        try
        {
            var refreshed = await refreshCoordinator.RefreshAsync(
                refreshToken,
                context.HttpContext.RequestAborted);
            _ = context.Properties.UpdateTokenValue("access_token", refreshed.AccessToken);
            _ = context.Properties.UpdateTokenValue("refresh_token", refreshed.RefreshToken);
            _ = context.Properties.UpdateTokenValue(
                "expires_at",
                refreshed.ExpiresAt.ToString("O", CultureInfo.InvariantCulture));
            context.ShouldRenew = true;
            RecordRefresh(startedAt, TokenRefreshOutcome.Success);
        }
        catch (TokenRefreshRejectedException exception)
        {
            RecordRefresh(startedAt, TokenRefreshOutcome.Rejected);
            s_logRefreshFailure(logger, exception);
            await RejectSessionAsync(context, "A renovação da sessão foi rejeitada.");
        }
        catch (OperationCanceledException exception) when (!context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            RecordRefresh(startedAt, TokenRefreshOutcome.TransientFailure);
            HandleTransientFailure(context, exception, hasExpiration, expiresAt);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            RecordRefresh(startedAt, TokenRefreshOutcome.TransientFailure);
            HandleTransientFailure(context, exception, hasExpiration, expiresAt);
        }
    }

    private void RecordRefresh(long startedAt, TokenRefreshOutcome outcome) =>
        telemetry.RecordTokenRefresh(timeProvider.GetElapsedTime(startedAt), outcome);

    private void HandleTransientFailure(
        CookieValidatePrincipalContext context,
        Exception exception,
        bool hasExpiration,
        DateTimeOffset expiresAt)
    {
        s_logRefreshFailure(logger, exception);
        if (!hasExpiration || expiresAt <= timeProvider.GetUtcNow())
        {
            context.RejectPrincipal();
        }
    }

    private static async Task RejectSessionAsync(
        CookieValidatePrincipalContext context,
        string reason)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.HttpContext.Items["Altivy.AuthenticationFailure"] = reason;
    }
}
