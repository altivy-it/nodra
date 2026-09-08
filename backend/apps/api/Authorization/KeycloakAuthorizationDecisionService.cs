using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Altivy.Notes.Api.Authorization;

public sealed partial class KeycloakAuthorizationDecisionService(
    HttpClient httpClient,
    KeycloakAuthorizationOptions options,
    ILogger<KeycloakAuthorizationDecisionService> logger) : IAuthorizationDecisionService
{
    private readonly KeycloakAuthorizationOptions authorizationOptions = options;

    public async Task<AuthorizationDecision> EvaluateAsync(
        string accessToken,
        string resource,
        string scope,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{authorizationOptions.Authority.TrimEnd('/')}/protocol/openid-connect/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:uma-ticket",
            ["audience"] = authorizationOptions.Audience,
            ["permission"] = $"{resource}#{scope}",
            ["response_mode"] = "decision"
        });

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return LogDecision(
                    new AuthorizationDecision(false, true, (int)response.StatusCode),
                    resource,
                    scope,
                    stopwatch);
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
            var result = document.RootElement.GetProperty("result").GetBoolean();
            return LogDecision(
                new AuthorizationDecision(result, true, (int)response.StatusCode),
                resource,
                scope,
                stopwatch);
        }
        catch (HttpRequestException)
        {
            return LogDecision(
                new AuthorizationDecision(false, false, StatusCodes.Status503ServiceUnavailable),
                resource,
                scope,
                stopwatch);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return LogDecision(
                new AuthorizationDecision(false, false, StatusCodes.Status503ServiceUnavailable),
                resource,
                scope,
                stopwatch);
        }
        catch (JsonException)
        {
            return LogDecision(
                new AuthorizationDecision(false, false, StatusCodes.Status503ServiceUnavailable),
                resource,
                scope,
                stopwatch);
        }
    }

    private AuthorizationDecision LogDecision(
        AuthorizationDecision decision,
        string resource,
        string scope,
        Stopwatch stopwatch)
    {
        stopwatch.Stop();
        DecisionLogged(
            logger,
            decision.Allowed ? "grant" : "deny",
            decision.Available,
            decision.StatusCode,
            resource,
            scope,
            stopwatch.Elapsed.TotalMilliseconds);
        return decision;
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Keycloak authorization decision outcome={Outcome} available={Available} status_code={StatusCode} resource={Resource} scope={Scope} duration_ms={DurationMs}")]
    private static partial void DecisionLogged(
        ILogger logger,
        string outcome,
        bool available,
        int statusCode,
        string resource,
        string scope,
        double durationMs);
}
