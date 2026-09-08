using Altivy.Bff.Relay;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Altivy.Bff.UnitTests.Relay;

[Trait("Category", "Unit")]
public sealed class BffHubsProxyRouteBuilderExtensionsTests
{
    [Fact]
    public void BuildDownstreamUriPreservesPathAndQuery()
    {
        var result = BffHubsProxyRouteBuilderExtensions.BuildDownstreamUri(
            new Uri("https://api.example/"),
            "vaults",
            new QueryString("?id=connection-1"));

        Assert.Equal("https://api.example/hubs/vaults?id=connection-1", result.AbsoluteUri);
    }

    [Fact]
    public void BuildDownstreamUriThrowsWhenBaseAddressMissing()
    {
        Assert.Throws<InvalidOperationException>(() =>
            BffHubsProxyRouteBuilderExtensions.BuildDownstreamUri(null, "vaults", QueryString.Empty));
    }

    [Theory]
    [InlineData("https://api.example/", "wss")]
    [InlineData("http://api.example/", "ws")]
    public void BuildDownstreamWebSocketUriTranslatesScheme(string baseAddress, string expectedScheme)
    {
        var result = BffHubsProxyRouteBuilderExtensions.BuildDownstreamWebSocketUri(
            new Uri(baseAddress),
            "vaults",
            QueryString.Empty);

        Assert.Equal(expectedScheme, result.Scheme);
        Assert.Equal("/hubs/vaults", result.AbsolutePath);
    }
}
