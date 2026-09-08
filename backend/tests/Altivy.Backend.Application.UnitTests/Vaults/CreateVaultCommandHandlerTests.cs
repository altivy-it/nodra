using Altivy.Backend.Application.Vaults;
using Altivy.Backend.Domain.Vaults;
using Xunit;

namespace Altivy.Backend.Application.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class CreateVaultCommandHandlerTests
{
    [Fact]
    public async Task HandleAsyncWithNewKeyCreatesPendingOperation()
    {
        var store = new FakeProvisioningStore();
        var handler = new CreateVaultCommandHandler(
            store,
            new FakeVaultIdGenerator("vault-0001"),
            () => Guid.Parse("00000000-0000-0000-0000-000000000001"));

        var result = await handler.HandleAsync(
            new CreateVaultCommand("Pessoal", "subject-1", "request-1"),
            CancellationToken.None);

        Assert.False(result.Reused);
        Assert.Equal("vault-0001", result.VaultId);
        Assert.Equal(VaultLifecycleState.Pending, result.State);
        Assert.NotNull(store.Operation);
    }

    [Fact]
    public async Task HandleAsyncWithRepeatedKeyReusesExistingOperation()
    {
        var store = new FakeProvisioningStore
        {
            Operation = VaultProvisioningOperation.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000002"),
                "vault-existing", "Pessoal", "subject-1")
        };
        var handler = new CreateVaultCommandHandler(
            store,
            new FakeVaultIdGenerator("vault-new"),
            Guid.NewGuid);

        var result = await handler.HandleAsync(
            new CreateVaultCommand("Outro nome", "subject-1", "request-1"),
            CancellationToken.None);

        Assert.True(result.Reused);
        Assert.Equal("vault-existing", result.VaultId);
        Assert.Equal(0, store.AddCount);
    }

    private sealed class FakeProvisioningStore : IVaultProvisioningStore
    {
        public VaultProvisioningOperation? Operation { get; set; }
        public int AddCount { get; private set; }

        public Task<VaultProvisioningOperation?> FindByIdempotencyKeyAsync(
            string subject,
            string idempotencyKey,
            CancellationToken cancellationToken) => Task.FromResult(Operation);

        public Task AddAsync(
            VaultProvisioningOperation operation,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Operation = operation;
            AddCount++;
            return Task.CompletedTask;
        }

        public Task<VaultProvisioningWorkItem?> ClaimNextAsync(
            DateTimeOffset now,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken) => Task.FromResult<VaultProvisioningWorkItem?>(null);

        public Task<bool> CompleteAsync(
            VaultProvisioningWorkItem workItem,
            DateTimeOffset? nextAttemptAt,
            string? lastError,
            CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<IReadOnlyList<string>> ListReadyVaultIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task RegisterMissingDirectoryAsync(
            string vaultId,
            DateTimeOffset detectedAt,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlySet<string>> ListKnownVaultIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());

        public Task RegisterOrphanedDirectoryAsync(
            string vaultId,
            DateTimeOffset detectedAt,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeVaultIdGenerator(string id) : IVaultIdGenerator
    {
        public string Generate() => id;
    }
}
