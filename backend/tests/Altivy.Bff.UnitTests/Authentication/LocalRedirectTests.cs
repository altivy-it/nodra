using Altivy.Bff.Authentication;

namespace Altivy.Bff.UnitTests.Authentication;

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
}