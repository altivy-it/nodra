using Nodra.Bff.Authentication;

namespace Nodra.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class LocalRedirectTests
{
    [Fact]
    public void NormalizeWithLocalPathPreservesPath()
    {
        var result = LocalRedirect.Normalize("/vaults/123?tab=notes");

        Assert.Equal("/vaults/123?tab=notes", result);
    }

    [Fact]
    public void NormalizeWithAbsoluteUrlReturnsRoot()
    {
        var result = LocalRedirect.Normalize("https://attacker.example");

        Assert.Equal("/", result);
    }

    [Fact]
    public void NormalizeWithProtocolRelativeUrlReturnsRoot()
    {
        var result = LocalRedirect.Normalize("//attacker.example");

        Assert.Equal("/", result);
    }

    [Theory]
    [InlineData(null, "/nodra/", "/nodra/")]
    [InlineData("https://attacker.example", "/nodra/", "/nodra/")]
    [InlineData("/nodra/workspace/lab-0001/files", "/nodra/", "/nodra/workspace/lab-0001/files")]
    public void NormalizeUsesSafeFallbackWithinApplicationBasePath(
        string? returnUrl,
        string fallbackUrl,
        string expected)
    {
        Assert.Equal(expected, LocalRedirect.Normalize(returnUrl, fallbackUrl));
    }
}
