using System.Security.Claims;
using Altivy.Bff.Authentication;
using Microsoft.AspNetCore.Authentication;

namespace Altivy.Bff.UnitTests.Authentication;

public sealed class ReauthenticationEvidenceTests
{
    [Fact]
    public void CreatesEvidenceFromOidcAuthTimeAndChecksFreshness()
    {
        var authenticatedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "subject-1"),
            new Claim("auth_time", authenticatedAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture))
        ]));

        var created = ReauthenticationEvidence.TryCreate(principal, out var evidence);

        Assert.True(created);
        Assert.NotNull(evidence);
        Assert.True(evidence!.IsFresh(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5)));
        Assert.False(evidence.IsFresh(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void ReadsOnlyEvidencePersistedInServerSideProperties()
    {
        var properties = new AuthenticationProperties();
        properties.Items[ReauthenticationEvidence.AuthenticatedAtItem] =
            DateTimeOffset.UtcNow.AddSeconds(-10).ToString("O");
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "subject-1")]));

        var created = ReauthenticationEvidence.TryRead(properties, principal, out var evidence);

        Assert.True(created);
        Assert.Equal("subject-1", evidence!.Subject);
    }

    [Fact]
    public void RejectsMissingOrInvalidAuthTime()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "subject-1")]));

        Assert.False(ReauthenticationEvidence.TryCreate(principal, out _));
    }
}
