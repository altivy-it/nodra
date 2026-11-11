using Nodra.Backend.Application.Vaults;
using Nodra.Backend.Domain.Vaults;
using Xunit;

namespace Nodra.Backend.Application.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class ListVaultCandidatesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsyncDelegatesCursorLimitAndCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var expected = new VaultCandidatePage([new("vault-a", "Vault A")], "next");
        var reader = new StubVaultReader(expected);
        var handler = new ListVaultCandidatesQueryHandler(reader);

        var result = await handler.HandleAsync(
            new ListVaultCandidatesQuery("cursor", 20),
            cancellation.Token);

        Assert.Same(expected, result);
        Assert.Equal("cursor", reader.Cursor);
        Assert.Equal(20, reader.Limit);
        Assert.Equal(cancellation.Token, reader.CancellationToken);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task HandleAsyncWithInvalidLimitThrows(int limit)
    {
        var handler = new ListVaultCandidatesQueryHandler(
            new StubVaultReader(new VaultCandidatePage([], null)));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.HandleAsync(
                new ListVaultCandidatesQuery(null, limit),
                CancellationToken.None));
    }

    private sealed class StubVaultReader(VaultCandidatePage page) : IVaultReader
    {
        public string? Cursor { get; private set; }

        public int Limit { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<Vault?> FindAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult<Vault?>(null);

        public Task<VaultCandidatePage> ReadCandidatesAsync(
            string? cursor,
            int limit,
            CancellationToken cancellationToken)
        {
            Cursor = cursor;
            Limit = limit;
            CancellationToken = cancellationToken;
            return Task.FromResult(page);
        }
    }
}