using Nodra.Backend.Application.Vaults;
using Xunit;

namespace Nodra.Backend.Application.UnitTests.Vaults;

[Trait("Category", "Unit")]
public sealed class VaultResidueAlertProcessorTests
{
    [Fact]
    public async Task ProcessNextAsyncWithSuccessfulDeliveryCompletesAlert()
    {
        var store = new FakeAlertStore(CreateWorkItem());
        var processor = new VaultResidueAlertProcessor(store, new FakeNotifier(), TimeProvider.System);

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        Assert.True(processed);
        Assert.Null(store.NextAttemptAt);
        Assert.Null(store.LastError);
    }

    [Fact]
    public async Task ProcessNextAsyncWithNotificationFailureSchedulesSanitizedRetry()
    {
        var store = new FakeAlertStore(CreateWorkItem());
        var processor = new VaultResidueAlertProcessor(
            store,
            new FakeNotifier(new AdministrativeNotificationDeliveryException()),
            TimeProvider.System);

        var processed = await processor.ProcessNextAsync(CancellationToken.None);

        Assert.True(processed);
        _ = Assert.NotNull(store.NextAttemptAt);
        Assert.Equal("administrative-notification-delivery-failed", store.LastError);
    }

    private static VaultResidueAlertWorkItem CreateWorkItem() => new(
        new VaultResidueAlert(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "vault-0001",
            "missing-directory/v1",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            1,
            "investigate-vault-residue"),
        Guid.Parse("00000000-0000-0000-0000-000000000002"));

    private sealed class FakeAlertStore(VaultResidueAlertWorkItem? workItem) : IVaultResidueAlertStore
    {
        public DateTimeOffset? NextAttemptAt { get; private set; }
        public string? LastError { get; private set; }

        public Task<VaultResidueAlertWorkItem?> ClaimNextAlertAsync(
            DateTimeOffset now,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken)
        {
            var claimed = workItem;
            workItem = null;
            return Task.FromResult(claimed);
        }

        public Task<bool> CompleteAlertAsync(
            VaultResidueAlertWorkItem workItem,
            DateTimeOffset? nextAttemptAt,
            string? lastError,
            CancellationToken cancellationToken)
        {
            NextAttemptAt = nextAttemptAt;
            LastError = lastError;
            return Task.FromResult(true);
        }

        public Task<int> ScheduleRetentionWarningsAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(0);
    }

    private sealed class FakeNotifier(Exception? exception = null) : IAdministrativeNotifier
    {
        public Task NotifyAsync(VaultResidueAlert alert, CancellationToken cancellationToken) =>
            exception is null ? Task.CompletedTask : Task.FromException(exception);
    }
}
