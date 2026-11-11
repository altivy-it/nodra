using Nodra.Backend.Application.Notes;
using Nodra.Backend.Domain.Notes;
using Xunit;

namespace Nodra.Backend.Application.UnitTests.Notes;

[Trait("Category", "Unit")]
public sealed class CreateNoteCommandHandlerTests
{
    [Fact]
    public async Task HandleAsyncWithAvailablePathCreatesNote()
    {
        var store = new StubNoteStore(true);
        var handler = new CreateNoteCommandHandler(store);

        var result = await handler.HandleAsync(
            new CreateNoteCommand("lab-0001", "projetos/plano.md", "# Plano"),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal("lab-0001", store.VaultId);
        Assert.Equal("projetos/plano.md", store.Path?.Value);
        Assert.Equal("# Plano", store.Content);
    }

    [Fact]
    public async Task HandleAsyncWithExistingPathReturnsFalse()
    {
        var handler = new CreateNoteCommandHandler(new StubNoteStore(false));

        var result = await handler.HandleAsync(
            new CreateNoteCommand("lab-0001", "nota.md", string.Empty),
            CancellationToken.None);

        Assert.False(result);
    }

    private sealed class StubNoteStore(bool result) : INoteStore
    {
        public string? VaultId { get; private set; }

        public NotePath? Path { get; private set; }

        public string? Content { get; private set; }

        public Task<IReadOnlyList<NoteDocument>> ListAsync(string vaultId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NoteDocument>>([]);

        public Task<NoteDocument?> FindAsync(string vaultId, NotePath path, CancellationToken cancellationToken) =>
            Task.FromResult<NoteDocument?>(null);

        public Task<bool> TryCreateAsync(
            string vaultId,
            NotePath path,
            string content,
            CancellationToken cancellationToken)
        {
            VaultId = vaultId;
            Path = path;
            Content = content;
            return Task.FromResult(result);
        }

        public Task<NoteWriteResult> UpdateAsync(
            string vaultId,
            NotePath path,
            string content,
            string expectedETag,
            CancellationToken cancellationToken) =>
            Task.FromResult(NoteWriteResult.NotFound);

        public Task<NoteWriteResult> DeleteAsync(
            string vaultId,
            NotePath path,
            string expectedETag,
            CancellationToken cancellationToken) =>
            Task.FromResult(NoteWriteResult.NotFound);

        public Task<NoteMoveResult> MoveAsync(
            string vaultId,
            NotePath source,
            NotePath destination,
            string expectedETag,
            CancellationToken cancellationToken) =>
            Task.FromResult(NoteMoveResult.NotFound);
    }
}
