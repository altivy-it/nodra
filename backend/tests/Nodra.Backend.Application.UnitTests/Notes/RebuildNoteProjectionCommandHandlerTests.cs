using Nodra.Backend.Application.Notes;
using Nodra.Backend.Domain.Notes;
using Xunit;

namespace Nodra.Backend.Application.UnitTests.Notes;

[Trait("Category", "Unit")]
public sealed class RebuildNoteProjectionCommandHandlerTests
{
    [Fact]
    public async Task HandleProjectsEveryCanonicalNote()
    {
        var notes = new[]
        {
            new NoteDocument(NotePath.Create("a.md"), "# A", "a"),
            new NoteDocument(NotePath.Create("b.md"), "# B", "b")
        };
        var projectionStore = new RecordingProjectionStore();
        var handler = new RebuildNoteProjectionCommandHandler(new StubNoteStore(notes), projectionStore);

        var count = await handler.HandleAsync("lab-0001", CancellationToken.None);

        Assert.Equal(2, count);
        Assert.Equal(["a.md", "b.md"], projectionStore.Paths);
    }

    private sealed class RecordingProjectionStore : INoteProjectionStore
    {
        public List<string> Paths { get; } = [];

        public Task UpsertAsync(string vaultId, NoteDocument note, CancellationToken cancellationToken)
        {
            Paths.Add(note.Path.Value);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string vaultId, NotePath path, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class StubNoteStore(IReadOnlyList<NoteDocument> notes) : INoteStore
    {
        public Task<IReadOnlyList<NoteDocument>> ListAsync(string vaultId, CancellationToken cancellationToken) =>
            Task.FromResult(notes);

        public Task<NoteDocument?> FindAsync(string vaultId, NotePath path, CancellationToken cancellationToken) =>
            Task.FromResult<NoteDocument?>(null);

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