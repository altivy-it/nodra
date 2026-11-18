using Nodra.Backend.Application.Vaults;
using Xunit;

namespace Nodra.Backend.Application.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class VaultAuthorizationTests
{
    [Fact]
    public void ResourceForReturnsTechnicalResourceId()
    {
        var resource = VaultAuthorization.ResourceFor("lab-0001");

        Assert.Equal("vault-lab-0001", resource);
    }

    [Fact]
    public void ReadScopeRemainsStable()
    {
        Assert.Equal("vault:read", VaultAuthorization.ReadScope);
    }

    [Theory]
    [InlineData("vault:write")]
    [InlineData("vault:manage")]
    [InlineData("vault:delete")]
    public void AdditionalScopesRemainStable(string expectedScope)
    {
        var actualScope = expectedScope switch
        {
            "vault:write" => VaultAuthorization.WriteScope,
            "vault:manage" => VaultAuthorization.ManageScope,
            "vault:delete" => VaultAuthorization.DeleteScope,
            _ => throw new ArgumentOutOfRangeException(nameof(expectedScope))
        };

        Assert.Equal(expectedScope, actualScope);
    }
}
