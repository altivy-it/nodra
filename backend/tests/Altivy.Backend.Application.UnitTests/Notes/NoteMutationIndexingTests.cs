using Altivy.Backend.Application.Notes;
using Altivy.Backend.Domain.Notes;
using Xunit;

namespace Altivy.Backend.Application.UnitTests.Notes;

[Trait("Category", "Unit")]
public sealed class NoteMutationIndexingTests
{
    [Fact]
    public async Task CreateEnqueuesOnlyAfterSuccessfulCreate()
    {
        var queue = new RecordingJobStore();
        var handler = new CreateNoteCommandHandler(new MutableNoteStore { CreateResult = true }, queue);

        var result = await handler.HandleAsync(
            new CreateNoteCommand("lab-0001", "plano.md", "# Plano"),
            CancellationToken.None);

        Assert.True(result);
        var job = Assert.Single(queue.Jobs);
        Assert.Equal("lab-0001", job.VaultId);
        Assert.Equal("plano.md", job.Path);
    }

    [Fact]
    public async Task UpdateAndDeleteEnqueueOnlySuccessfulMutations()
    {
        var queue = new RecordingJobStore();
        var store = new MutableNoteStore
        {
            UpdateResult = NoteWriteResult.Updated,
            DeleteResult = NoteWriteResult.Updated
        };

        var update = new UpdateNoteCommandHandler(store, queue);
        var delete = new DeleteNoteCommandHandler(store, queue);

        Assert.Equal(
            NoteWriteResult.Updated,
            await update.HandleAsync("lab-0001", "plano.md", "# Novo", "etag", CancellationToken.None));
        Assert.Equal(
            NoteWriteResult.Updated,
            await delete.HandleAsync("lab-0001", "plano.md", "etag", CancellationToken.None));

        Assert.Equal(2, queue.Jobs.Count);
        Assert.Equal("plano.md", queue.Jobs[0].Path);
        Assert.Equal("plano.md", queue.Jobs[1].Path);
    }

    private sealed class RecordingJobStore : INoteIndexJobStore
    {
        public List<(string VaultId, string? Path, string Key)> Jobs { get; } = [];

        public Task EnqueueAsync(string vaultId, string? path, string idempotencyKey, CancellationToken cancellationToken)
        {
            Jobs.Add((vaultId, path, idempotencyKey));
            return Task.CompletedTask;
        }

        public Task<NoteIndexJob?> ClaimNextAsync(DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken) =>
            Task.FromResult<NoteIndexJob?>(null);

        public Task<bool> CompleteAsync(NoteIndexJob job, DateTimeOffset? nextAttemptAt, string? lastError, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class MutableNoteStore : INoteStore
    {
        public bool CreateResult { get; init; }

        public NoteWriteResult UpdateResult { get; init; } = NoteWriteResult.NotFound;

        public NoteWriteResult DeleteResult { get; init; } = NoteWriteResult.NotFound;

        public Task<IReadOnlyList<NoteDocument>> ListAsync(string vaultId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NoteDocument>>([]);

        public Task<NoteDocument?> FindAsync(string vaultId, NotePath path, CancellationToken cancellationToken) =>
            Task.FromResult<NoteDocument?>(null);

        public Task<bool> TryCreateAsync(string vaultId, NotePath path, string content, CancellationToken cancellationToken) =>
            Task.FromResult(CreateResult);

        public Task<NoteWriteResult> UpdateAsync(string vaultId, NotePath path, string content, string expectedETag, CancellationToken cancellationToken) =>
            Task.FromResult(UpdateResult);

        public Task<NoteWriteResult> DeleteAsync(string vaultId, NotePath path, string expectedETag, CancellationToken cancellationToken) =>
            Task.FromResult(DeleteResult);
    }
}