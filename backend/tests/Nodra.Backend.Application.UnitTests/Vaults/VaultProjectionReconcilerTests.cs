using Nodra.Backend.Application.Vaults;
using Nodra.Backend.Domain.Vaults;
using Xunit;

namespace Nodra.Backend.Application.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class VaultProjectionReconcilerTests
{
    [Fact]
    public async Task ReconcileAsyncMarksOnlyVaultsWithoutDirectoryAsDegraded()
    {
        var store = new FakeProvisioningStore(["vault-present", "vault-missing"]);
        var reconciler = new VaultProjectionReconciler(
            store,
            new FakeFileSystemProvisioner(new HashSet<string> { "vault-present" }),
            TimeProvider.System);

        var degradedCount = await reconciler.ReconcileAsync(CancellationToken.None);

        Assert.Equal(1, degradedCount);
        Assert.Equal(["vault-missing"], store.MissingDirectoryVaultIds);
    }

    private sealed class FakeProvisioningStore(IReadOnlyList<string> readyVaultIds) : IVaultProvisioningStore
    {
        public List<string> MissingDirectoryVaultIds { get; } = [];

        public Task<VaultProvisioningOperation?> FindByIdempotencyKeyAsync(
            string subject,
            string idempotencyKey,
            CancellationToken cancellationToken) => Task.FromResult<VaultProvisioningOperation?>(null);

        public Task AddAsync(
            VaultProvisioningOperation operation,
            string idempotencyKey,
            CancellationToken cancellationToken) => Task.CompletedTask;

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
            Task.FromResult(readyVaultIds);

        public Task RegisterMissingDirectoryAsync(
            string vaultId,
            DateTimeOffset detectedAt,
            CancellationToken cancellationToken)
        {
            MissingDirectoryVaultIds.Add(vaultId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlySet<string>> ListKnownVaultIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());

        public Task RegisterOrphanedDirectoryAsync(
            string vaultId,
            DateTimeOffset detectedAt,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeFileSystemProvisioner(IReadOnlySet<string> existingVaultIds) : IVaultFileSystemProvisioner
    {
        public Task ProvisionAsync(string vaultId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsAsync(string vaultId, CancellationToken cancellationToken) =>
            Task.FromResult(existingVaultIds.Contains(vaultId));

        public Task<IReadOnlyList<string>> ListVaultIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<bool> QuarantineAsync(string vaultId, DateTimeOffset detectedAt, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}
