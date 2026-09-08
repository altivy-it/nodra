using Altivy.Notes.Api.Vaults;
using Xunit;

namespace Altivy.Notes.Api.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class VaultIdTests
{
    [Theory]
    [InlineData("lab-0001")]
    [InlineData("vault-01")]
    public void TryParseWithSafeIdentifierReturnsValue(string value)
    {
        var parsed = VaultId.TryParse(value, out var vaultId);

        Assert.True(parsed);
        Assert.Equal(value, vaultId.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../secret")]
    [InlineData("Vault-01")]
    [InlineData("lab_01")]
    public void TryParseWithInvalidIdentifierReturnsFalse(string value)
    {
        var parsed = VaultId.TryParse(value, out _);

        Assert.False(parsed);
    }
}
