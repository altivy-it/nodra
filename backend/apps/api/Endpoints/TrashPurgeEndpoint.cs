using System.Globalization;
using Altivy.Backend.Application.Notes;
using Altivy.Backend.Application.Vaults;
using Altivy.Notes.Api.Authorization;
using Altivy.Notes.Api.Vaults;

namespace Altivy.Notes.Api.Endpoints;

internal static class TrashPurgeEndpoint
{
    internal static async Task<IResult> HandleAsync(
        string vaultId,
        HttpContext context,
        IAuthorizationDecisionService authorization,
        EmptyTrashCommandHandler command,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.DeleteScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        var subject = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
        if (!TryReadFreshReauthentication(context, out var reauthenticatedAtUtc))
        {
            return Results.Problem(statusCode: 428, title: "Reautenticação recente é obrigatória.",
                extensions: new Dictionary<string, object?> { ["code"] = "reauthentication-required" });
        }

        try
        {
            var result = await command.HandleAsync(vaultId, subject, reauthenticatedAtUtc, cancellationToken);
            return Results.Ok(new { removedOperations = result.RemovedOperations, failedOperations = result.FailedOperations });
        }
        catch (InvalidOperationException)
        {
            return Results.Problem(statusCode: 503, title: "Auditoria da lixeira indisponível.");
        }
    }

    private static async Task<IResult?> AuthorizeAsync(
        string vaultId,
        string scope,
        HttpContext context,
        IAuthorizationDecisionService authorization,
        CancellationToken cancellationToken)
    {
        if (!VaultId.TryParse(vaultId, out var parsedVaultId))
        {
            return Results.Problem(statusCode: 400, title: "Identificador de vault inválido.");
        }
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Results.Unauthorized();
        var decision = await authorization.EvaluateAsync(
            header["Bearer ".Length..].Trim(),
            VaultAuthorization.ResourceFor(parsedVaultId.Value),
            scope,
            cancellationToken);
        if (!decision.Available) return Results.Problem(statusCode: 503, title: "Serviço de autorização indisponível.");
        return decision.Allowed ? null : Results.Problem(statusCode: 403, title: "Acesso negado.");
    }

    private static bool TryReadFreshReauthentication(HttpContext context, out DateTimeOffset authenticatedAtUtc)
    {
        authenticatedAtUtc = default;
        var value = context.Request.Headers["X-Altivy-Reauth-At"].ToString();
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out authenticatedAtUtc)) return false;
        var now = DateTimeOffset.UtcNow;
        return authenticatedAtUtc <= now.AddSeconds(30) && now - authenticatedAtUtc <= TimeSpan.FromMinutes(5);
    }
}
