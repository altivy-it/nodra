using Altivy.Backend.Domain.Vaults;
using Xunit;

namespace Altivy.Backend.Domain.UnitTests.Vaults;

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
        Assert.Throws<ArgumentException>(() => Vault.Create(id, "Laboratório"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateWithInvalidNameThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => Vault.Create("lab-0001", name));
    }
}
