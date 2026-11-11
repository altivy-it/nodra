using System.Security.Claims;
using Nodra.Bff.Endpoints;
using Xunit;

namespace Nodra.Bff.UnitTests.Endpoints;

[Trait("Category", "Unit")]
public sealed class BffSessionResponseTests
{
    [Fact]
    public void FromPrincipalWithAuthenticatedIdentityReturnsTechnicalSession()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "synthetic-sub"),
            new Claim(ClaimTypes.Name, "Pessoa Sintética"),
            new Claim("access_token", "must-not-be-projected")
        ],
        "Test"));

        var response = BffSessionResponse.FromPrincipal(principal);

        Assert.True(response.Authenticated);
        Assert.Equal("synthetic-sub", response.Subject);
        Assert.Equal("Pessoa Sintética", response.Name);
    }

    [Fact]
    public void FromPrincipalWithAnonymousIdentityDoesNotExposeClaims()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("sub", "anonymous-sub")],
        authenticationType: null));

        var response = BffSessionResponse.FromPrincipal(principal);

        Assert.False(response.Authenticated);
        Assert.Null(response.Subject);
        Assert.Null(response.Name);
    }
}
