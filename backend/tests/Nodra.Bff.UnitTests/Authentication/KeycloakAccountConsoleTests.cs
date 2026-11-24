using Nodra.Bff.Authentication;

namespace Nodra.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class KeycloakAccountConsoleTests
{
    [Theory]
    [InlineData("https://localhost:7443/realms/altivy")]
    [InlineData("https://localhost:7443/realms/altivy/")]
    public void BuildUrlPointsToRealmAccountConsoleWithClientReferrer(string authority)
    {
        var result = KeycloakAccountConsole.BuildUrl(authority, "altivy-bff");

        Assert.Equal("https://localhost:7443/realms/altivy/account/?referrer=altivy-bff", result);
    }

    [Fact]
    public void BuildUrlEscapesClientId()
    {
        var result = KeycloakAccountConsole.BuildUrl("https://id/realms/r", "a&b=c");

        Assert.EndsWith("?referrer=a%26b%3Dc", result);
    }
}
