using Altivy.Backend.Application.Vaults;
using Altivy.Backend.Domain.Vaults;
using Xunit;

namespace Altivy.Backend.Application.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class VaultProvisioningProcessorTests
{
    [Fact]
    public async Task ProcessNextAsyncWithProvisionedDirectoryMarksOperationReady()
    {
        var store = new FakeProvisioningStore(CreateWorkItem());
        var processor = new VaultProvisioningProcessor(store, new FakeFileSystemProvisioner(), TimeProvider.System);

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        Assert.True(processed);
        Assert.NotNull(store.CompletedWorkItem);
        Assert.Equal(VaultLifecycleState.Ready, store.CompletedWorkItem.Operation.State);
        Assert.Null(store.NextAttemptAt);
        Assert.Null(store.LastError);
    }

    [Fact]
    public async Task ProcessNextAsyncWithIoFailureSchedulesRetryWithoutPersistingExceptionDetails()
    {
        var store = new FakeProvisioningStore(CreateWorkItem());
        var processor = new VaultProvisioningProcessor(
            store,
            new FakeFileSystemProvisioner(new IOException("internal path")),
            TimeProvider.System);

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        Assert.True(processed);
        Assert.NotNull(store.CompletedWorkItem);
        Assert.Equal(VaultLifecycleState.Degraded, store.CompletedWorkItem.Operation.State);
        Assert.NotNull(store.NextAttemptAt);
        Assert.Equal("filesystem-provisioning-failed", store.LastError);
    }

    [Fact]
    public async Task ProcessNextAsyncWithoutEligibleOperationReturnsFalse()
    {
        var store = new FakeProvisioningStore(null);
        var processor = new VaultProvisioningProcessor(store, new FakeFileSystemProvisioner(), TimeProvider.System);

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        Assert.False(processed);
        Assert.Null(store.CompletedWorkItem);
    }

    private static VaultProvisioningWorkItem CreateWorkItem()
    {
        var operation = VaultProvisioningOperation.Create(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "vault-0001",
            "Pessoal",
            "subject-1");
        operation.TransitionTo(VaultLifecycleState.Provisioning);
        return new VaultProvisioningWorkItem(operation, Guid.NewGuid(), 1);
    }

    private sealed class FakeProvisioningStore(VaultProvisioningWorkItem? workItem) : IVaultProvisioningStore
    {
        public VaultProvisioningWorkItem? CompletedWorkItem { get; private set; }
        public DateTimeOffset? NextAttemptAt { get; private set; }
        public string? LastError { get; private set; }

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
            CancellationToken cancellationToken)
        {
            var claimed = workItem;
            workItem = null;
            return Task.FromResult(claimed);
        }

        public Task<bool> CompleteAsync(
            VaultProvisioningWorkItem completedWorkItem,
            DateTimeOffset? nextAttemptAt,
            string? lastError,
            CancellationToken cancellationToken)
        {
            CompletedWorkItem = completedWorkItem;
            NextAttemptAt = nextAttemptAt;
            LastError = lastError;
            return Task.FromResult(true);
        }

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

    private sealed class FakeFileSystemProvisioner(Exception? exception = null) : IVaultFileSystemProvisioner
    {
        public Task ProvisionAsync(string vaultId, CancellationToken cancellationToken) =>
            exception is null ? Task.CompletedTask : Task.FromException(exception);

        public Task<bool> ExistsAsync(string vaultId, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<IReadOnlyList<string>> ListVaultIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<bool> QuarantineAsync(
            string vaultId,
            DateTimeOffset detectedAt,
            CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
