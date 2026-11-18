using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Nodra.Bff.Authentication;

namespace Nodra.Bff.UnitTests.Authentication;

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

    [Fact]
    public void RejectsOidcEvidenceForADifferentSubject()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "subject-2"),
            new Claim("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture))
        ]));

        Assert.False(ReauthenticationEvidence.TryCreate(principal, "subject-1", out _));
        Assert.True(ReauthenticationEvidence.TryCreate(principal, "subject-2", out var evidence));
        Assert.Equal("subject-2", evidence!.Subject);
    }

    [Fact]
    public void AllowsLimitedClockSkewAndRejectsFarFutureAuthTime()
    {
        var now = DateTimeOffset.UtcNow;
        var evidence = new ReauthenticationEvidence("subject-1", now.AddSeconds(20));

        Assert.True(evidence.IsFresh(now, TimeSpan.FromMinutes(5)));
        Assert.False(new ReauthenticationEvidence("subject-1", now.AddSeconds(31))
            .IsFresh(now, TimeSpan.FromMinutes(5)));
    }
}
