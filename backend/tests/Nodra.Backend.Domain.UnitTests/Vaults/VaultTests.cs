using Nodra.Backend.Domain.Vaults;
using Xunit;

namespace Nodra.Backend.Domain.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class VaultTests
{
    [Fact]
    public void CreateWithValidValuesReturnsVault()
    {
        var vault = Vault.Create("lab-0001", "  Laboratório  ");

        Assert.Equal("lab-0001", vault.Id);
        Assert.Equal("Laboratório", vault.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../secret")]
    [InlineData("Lab-0001")]
    [InlineData("lab_0001")]
    public void CreateWithInvalidIdThrowsArgumentException(string id)
    {
        _ = Assert.Throws<ArgumentException>(() => Vault.Create(id, "Laboratório"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateWithInvalidNameThrowsArgumentException(string name)
    {
        _ = Assert.Throws<ArgumentException>(() => Vault.Create("lab-0001", name));
    }
}
