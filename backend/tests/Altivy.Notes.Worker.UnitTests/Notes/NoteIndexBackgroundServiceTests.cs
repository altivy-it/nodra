using Altivy.Backend.Application.Notes;
using Altivy.Backend.Domain.Notes;
using Altivy.Notes.Worker.Notes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Altivy.Notes.Worker.UnitTests.Notes;

[Trait("Category", "Unit")]
public sealed class NoteIndexBackgroundServiceTests
{
    [Fact]
    public async Task RetriesTransientDatabaseFailureAndKeepsRunning()
    {
        var jobStore = new TransientOnceJobStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<INoteIndexJobStore>(jobStore);
        services.AddSingleton<INoteStore, UnusedNoteStore>();
        services.AddSingleton<INoteProjectionStore, UnusedProjectionStore>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<NoteIndexJobProcessor>();
        await using var provider = services.BuildServiceProvider();
        var service = new NoteIndexBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<NoteIndexBackgroundService>>());

        await service.StartAsync(CancellationToken.None);
        await jobStore.RetryObserved.Task.WaitAsync(TimeSpan.FromSeconds(20));
        await jobStore.StopObserved.Task.WaitAsync(TimeSpan.FromSeconds(20));
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(3, jobStore.ClaimAttempts);
        Assert.True(service.ExecuteTask?.IsCompleted);
    }

    private sealed class TransientOnceJobStore : INoteIndexJobStore
    {
        private int claimAttempts;

        public TaskCompletionSource RetryObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource StopObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ClaimAttempts => Volatile.Read(ref claimAttempts);

        public Task EnqueueAsync(string vaultId, string? path, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public async Task<NoteIndexJob?> ClaimNextAsync(
            DateTimeOffset now,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref claimAttempts);
            if (attempt == 1)
            {
                throw new InvalidOperationException("Transient database failure.", new TimeoutException());
            }

            if (attempt == 2)
            {
                RetryObserved.TrySetResult();
                return null;
            }

            StopObserved.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return null;
        }

        public Task<bool> CompleteAsync(
            NoteIndexJob job,
            DateTimeOffset? nextAttemptAt,
            string? lastError,
            CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class UnusedNoteStore : INoteStore
    {
        public Task<IReadOnlyList<NoteDocument>> ListAsync(string vaultId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<NoteDocument?> FindAsync(string vaultId, NotePath path, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> TryCreateAsync(string vaultId, NotePath path, string content, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<NoteWriteResult> UpdateAsync(
            string vaultId, NotePath path, string content, string expectedETag, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<NoteWriteResult> DeleteAsync(
            string vaultId, NotePath path, string expectedETag, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<NoteMoveResult> MoveAsync(
            string vaultId,
            NotePath source,
            NotePath destination,
            string expectedETag,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class UnusedProjectionStore : INoteProjectionStore
    {
        public Task UpsertAsync(string vaultId, NoteDocument note, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string vaultId, NotePath path, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
