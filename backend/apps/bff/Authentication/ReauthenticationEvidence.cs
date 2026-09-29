using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Altivy.Bff.Authentication;

/// <summary>
/// Evidência server-side de uma autenticação OIDC solicitada como step-up.
/// </summary>
public sealed record ReauthenticationEvidence(string Subject, DateTimeOffset AuthenticatedAtUtc)
{
    public const string ChallengeItem = "Altivy.Reauthentication.Challenge";
    public const string AuthenticatedAtItem = "Altivy.Reauthentication.AuthenticatedAtUtc";

    public static bool TryCreate(ClaimsPrincipal principal, out ReauthenticationEvidence? evidence)
    {
        evidence = null;
        var subject = principal.FindFirst("sub")?.Value;
        var authTime = principal.FindFirst("auth_time")?.Value;
        if (string.IsNullOrWhiteSpace(subject) || !long.TryParse(authTime, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            return false;
        }

        try
        {
            evidence = new ReauthenticationEvidence(
                subject,
                DateTimeOffset.FromUnixTimeSeconds(seconds).ToUniversalTime());
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static bool TryRead(
        AuthenticationProperties properties,
        ClaimsPrincipal principal,
        out ReauthenticationEvidence? evidence)
    {
        evidence = null;
        var subject = principal.FindFirst("sub")?.Value;
        properties.Items.TryGetValue(AuthenticatedAtItem, out var value);
        if (string.IsNullOrWhiteSpace(subject) || !DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var authenticatedAt))
        {
            return false;
        }

        evidence = new ReauthenticationEvidence(subject, authenticatedAt.ToUniversalTime());
        return true;
    }

    public bool IsFresh(DateTimeOffset nowUtc, TimeSpan maxAge) =>
        AuthenticatedAtUtc <= nowUtc && nowUtc - AuthenticatedAtUtc <= maxAge;
}
