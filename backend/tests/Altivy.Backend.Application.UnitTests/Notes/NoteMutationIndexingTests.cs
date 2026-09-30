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

    [Fact]
    public async Task MoveEnqueuesSourceAndDestinationReconciliationOnlyWhenMoved()
    {
        var queue = new RecordingJobStore();
        var moved = new MoveNoteCommandHandler(new MutableNoteStore { MoveResult = NoteMoveResult.Moved }, queue);
        var conflict = new MoveNoteCommandHandler(new MutableNoteStore { MoveResult = NoteMoveResult.DestinationExists }, queue);

        Assert.Equal(
            NoteMoveResult.Moved,
            await moved.HandleAsync("lab-0001", "rascunho.md", "projetos/plano.md", "etag", CancellationToken.None));
        Assert.Equal(
            NoteMoveResult.DestinationExists,
            await conflict.HandleAsync("lab-0001", "outra.md", "projetos/plano.md", "etag", CancellationToken.None));

        Assert.Collection(
            queue.Jobs,
            job => Assert.Equal("rascunho.md", job.Path),
            job => Assert.Equal("projetos/plano.md", job.Path));
        Assert.NotEqual(queue.Jobs[0].Key, queue.Jobs[1].Key);
    }

    [Fact]
    public async Task MoveRejectsIdenticalSourceAndDestination()
    {
        var handler = new MoveNoteCommandHandler(new MutableNoteStore(), new RecordingJobStore());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync("lab-0001", "plano.md", "plano.md", "etag", CancellationToken.None));
    }

    [Fact]
    public async Task RepeatedMoveBetweenSamePathsUsesDistinctIdempotencyKeys()
    {
        var queue = new RecordingJobStore();
        var handler = new MoveNoteCommandHandler(new MutableNoteStore { MoveResult = NoteMoveResult.Moved }, queue);

        await handler.HandleAsync("lab-0001", "a.md", "b.md", "etag", CancellationToken.None);
        await handler.HandleAsync("lab-0001", "a.md", "b.md", "etag", CancellationToken.None);

        Assert.Equal(4, queue.Jobs.Select(job => job.Key).Distinct().Count());
    }

    [Fact]
    public async Task FolderMoveEnqueuesEachMovedNoteAtSourceAndDestination()
    {
        var queue = new RecordingJobStore();
        var store = new MovingFolderStore(new FolderMoveOutcome(
            FolderMoveResult.Moved,
            [("projetos/plano.md", "arquivo/plano.md"), ("projetos/x/y.md", "arquivo/x/y.md")]));
        var handler = new MoveFolderCommandHandler(store, queue);

        var result = await handler.HandleAsync("lab-0001", "projetos", "arquivo", CancellationToken.None);

        Assert.Equal(FolderMoveResult.Moved, result);
        Assert.Equal(
            ["projetos/plano.md", "arquivo/plano.md", "projetos/x/y.md", "arquivo/x/y.md"],
            queue.Jobs.Select(job => job.Path));
        Assert.Equal(4, queue.Jobs.Select(job => job.Key).Distinct().Count());
    }

    [Theory]
    [InlineData("projetos", "projetos")]
    [InlineData("projetos", "projetos/interno")]
    [InlineData("projetos", "PROJETOS/interno")]
    public async Task FolderMoveRejectsDestinationEqualToOrInsideSource(string source, string destination)
    {
        var store = new MovingFolderStore(new FolderMoveOutcome(FolderMoveResult.Moved, []));
        var handler = new MoveFolderCommandHandler(store, new RecordingJobStore());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync("lab-0001", source, destination, CancellationToken.None));
        Assert.False(store.Called);
    }

    [Fact]
    public async Task FolderMoveAllowsSiblingWithSharedPrefix()
    {
        var store = new MovingFolderStore(new FolderMoveOutcome(FolderMoveResult.Moved, []));
        var handler = new MoveFolderCommandHandler(store, new RecordingJobStore());

        Assert.Equal(
            FolderMoveResult.Moved,
            await handler.HandleAsync("lab-0001", "projetos", "projetos-antigos", CancellationToken.None));
    }

    private sealed class MovingFolderStore(FolderMoveOutcome outcome) : IFolderStore
    {
        public bool Called { get; private set; }

        public Task<IReadOnlyList<VaultEntry>> ListChildrenAsync(string vaultId, FolderPath? parentPath, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VaultEntry>>([]);

        public Task<FolderCreationResult> EnsureCreatedAsync(string vaultId, FolderPath path, CancellationToken cancellationToken) =>
            Task.FromResult(FolderCreationResult.Created);

        public Task<FolderDeletionResult> DeleteEmptyAsync(string vaultId, FolderPath path, CancellationToken cancellationToken) =>
            Task.FromResult(FolderDeletionResult.Deleted);

        public Task<TrashMoveOutcome> MoveToTrashAsync(
            string vaultId,
            string subject,
            FolderPath path,
            string? expectedETag,
            CancellationToken cancellationToken,
            int? retentionDays = null) =>
            Task.FromResult(new TrashMoveOutcome(TrashMoveResult.NotFound));

        public Task<IReadOnlyList<TrashEntry>> ListTrashAsync(
            string vaultId,
            string subject,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TrashEntry>>([]);

        public Task<TrashRestoreOutcome> RestoreFromTrashAsync(
            string vaultId,
            string subject,
            Guid operationId,
            FolderPath destination,
            CancellationToken cancellationToken) =>
            Task.FromResult(new TrashRestoreOutcome(TrashRestoreResult.NotFound));

        public Task<TrashEmptyOutcome> EmptyTrashAsync(
            string vaultId,
            string subject,
            CancellationToken cancellationToken) =>
            Task.FromResult(new TrashEmptyOutcome(0, 0));

        public Task<FolderMoveOutcome> MoveAsync(string vaultId, FolderPath source, FolderPath destination, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(outcome);
        }
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

        public NoteMoveResult MoveResult { get; init; } = NoteMoveResult.NotFound;

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

        public Task<NoteMoveResult> MoveAsync(string vaultId, NotePath source, NotePath destination, string expectedETag, CancellationToken cancellationToken) =>
            Task.FromResult(MoveResult);
    }
}