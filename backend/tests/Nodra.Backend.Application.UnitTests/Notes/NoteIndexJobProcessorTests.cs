using Nodra.Backend.Application.Notes;
using Nodra.Backend.Domain.Notes;
using Xunit;

namespace Nodra.Backend.Application.UnitTests.Notes;

[Trait("Category", "Unit")]
public sealed class NoteIndexJobProcessorTests
{
    [Fact]
    public async Task ProcessNextRebuildsProjectionForWholeVaultJob()
    {
        var note = new NoteDocument(NotePath.Create("plano.md"), "# Plano", "etag");
        var jobs = new StubJobStore(new NoteIndexJob(Guid.NewGuid(), "lab-0001", null, 1, Guid.NewGuid()));
        var projections = new RecordingProjectionStore();
        var processor = new NoteIndexJobProcessor(
            jobs,
            new StubNoteStore(note),
            projections,
            TimeProvider.System);

        Assert.True(await processor.ProcessNextAsync(CancellationToken.None));
        Assert.Equal(["plano.md"], projections.Paths);
        Assert.True(jobs.Completed);
    }

    private sealed class StubJobStore(NoteIndexJob? job) : INoteIndexJobStore
    {
        public bool Completed { get; private set; }

        public Task EnqueueAsync(string vaultId, string? path, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<NoteIndexJob?> ClaimNextAsync(DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken) =>
            Task.FromResult(job);

        public Task<bool> CompleteAsync(NoteIndexJob job, DateTimeOffset? nextAttemptAt, string? lastError, CancellationToken cancellationToken)
        {
            Completed = true;
            return Task.FromResult(true);
        }
    }

    private sealed class RecordingProjectionStore : INoteProjectionStore
    {
        public List<string> Paths { get; } = [];

        public List<string> DeletedPaths { get; } = [];

        public Task UpsertAsync(string vaultId, NoteDocument note, CancellationToken cancellationToken)
        {
            Paths.Add(note.Path.Value);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string vaultId, NotePath path, CancellationToken cancellationToken)
        {
            DeletedPaths.Add(path.Value);
            return Task.CompletedTask;
        }
    }

    private sealed class StubNoteStore(NoteDocument note) : INoteStore
    {
        public Task<IReadOnlyList<NoteDocument>> ListAsync(string vaultId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NoteDocument>>([note]);

        public Task<NoteDocument?> FindAsync(string vaultId, NotePath path, CancellationToken cancellationToken) =>
            Task.FromResult<NoteDocument?>(note);

        public Task<bool> TryCreateAsync(string vaultId, NotePath path, string content, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<NoteWriteResult> UpdateAsync(string vaultId, NotePath path, string content, string expectedETag, CancellationToken cancellationToken) =>
            Task.FromResult(NoteWriteResult.NotFound);

        public Task<NoteWriteResult> DeleteAsync(string vaultId, NotePath path, string expectedETag, CancellationToken cancellationToken) =>
            Task.FromResult(NoteWriteResult.NotFound);

        public Task<NoteMoveResult> MoveAsync(string vaultId, NotePath source, NotePath destination, string expectedETag, CancellationToken cancellationToken) =>
            Task.FromResult(NoteMoveResult.NotFound);
    }
}