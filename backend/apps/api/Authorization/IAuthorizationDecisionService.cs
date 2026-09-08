namespace Altivy.Notes.Api.Authorization;

public interface IAuthorizationDecisionService
{
    Task<AuthorizationDecision> EvaluateAsync(
        string accessToken,
        string resource,
        string scope,
        CancellationToken cancellationToken);
}

public sealed record AuthorizationDecision(bool Allowed, bool Available, int StatusCode);
