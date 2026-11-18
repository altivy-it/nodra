using Nodra.Backend.Application.Vaults;
using Nodra.Backend.Domain.Vaults;
using Xunit;

namespace Nodra.Backend.Application.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class VaultOrphanDirectoryReconcilerTests
{
    [Fact]
    public async Task ReconcileAsyncQuarantinesOnlyUnknownVaultDirectories()
    {
        var store = new FakeProvisioningStore(new HashSet<string> { "vault-known" });
        var filesystem = new FakeFileSystemProvisioner(["vault-known", "vault-orphan"]);
        var reconciler = new VaultOrphanDirectoryReconciler(store, filesystem, TimeProvider.System);

        var quarantinedCount = await reconciler.ReconcileAsync(CancellationToken.None);

        Assert.Equal(1, quarantinedCount);
        Assert.Equal(["vault-orphan"], filesystem.QuarantinedVaultIds);
        Assert.Equal(["vault-orphan"], store.OrphanedVaultIds);
    }

    private sealed class FakeProvisioningStore(IReadOnlySet<string> knownVaultIds) : IVaultProvisioningStore
    {
        public List<string> OrphanedVaultIds { get; } = [];

        public Task<VaultProvisioningOperation?> FindByIdempotencyKeyAsync(string subject, string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult<VaultProvisioningOperation?>(null);
        public Task AddAsync(VaultProvisioningOperation operation, string idempotencyKey, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VaultProvisioningWorkItem?> ClaimNextAsync(DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken) => Task.FromResult<VaultProvisioningWorkItem?>(null);
        public Task<bool> CompleteAsync(VaultProvisioningWorkItem workItem, DateTimeOffset? nextAttemptAt, string? lastError, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<IReadOnlyList<string>> ListReadyVaultIdsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task RegisterMissingDirectoryAsync(string vaultId, DateTimeOffset detectedAt, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlySet<string>> ListKnownVaultIdsAsync(CancellationToken cancellationToken) => Task.FromResult(knownVaultIds);

        public Task RegisterOrphanedDirectoryAsync(string vaultId, DateTimeOffset detectedAt, CancellationToken cancellationToken)
        {
            OrphanedVaultIds.Add(vaultId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileSystemProvisioner(IReadOnlyList<string> vaultIds) : IVaultFileSystemProvisioner
    {
        public List<string> QuarantinedVaultIds { get; } = [];

        public Task ProvisionAsync(string vaultId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string vaultId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<IReadOnlyList<string>> ListVaultIdsAsync(CancellationToken cancellationToken) => Task.FromResult(vaultIds);

        public Task<bool> QuarantineAsync(string vaultId, DateTimeOffset detectedAt, CancellationToken cancellationToken)
        {
            QuarantinedVaultIds.Add(vaultId);
            return Task.FromResult(true);
        }
    }
}
