using Altivy.Backend.Application.Vaults;
using Altivy.Backend.Domain.Vaults;
using Xunit;

namespace Altivy.Backend.Application.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class GetVaultQueryHandlerTests
{
    [Fact]
    public async Task HandleAsyncWithExistingVaultReturnsVault()
    {
        var expected = Vault.Create("lab-0001", "Laboratório");
        var handler = new GetVaultQueryHandler(new StubVaultReader(expected));

        var result = await handler.HandleAsync("lab-0001", CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task HandleAsyncWithMissingVaultReturnsNull()
    {
        var handler = new GetVaultQueryHandler(new StubVaultReader(null));

        var result = await handler.HandleAsync("missing", CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class StubVaultReader(Vault? vault) : IVaultReader
    {
        public Task<Vault?> FindAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult(vault);

        public Task<VaultCandidatePage> ReadCandidatesAsync(
            string? cursor,
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult(new VaultCandidatePage([], null));
    }
}
