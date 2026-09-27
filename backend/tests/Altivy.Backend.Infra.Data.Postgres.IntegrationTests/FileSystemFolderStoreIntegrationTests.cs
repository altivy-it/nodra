using Altivy.Backend.Application.Notes;
using Altivy.Backend.Domain.Notes;
using Altivy.Backend.Infra.Data.Postgres;
using Xunit;

namespace Altivy.Backend.Infra.Data.Postgres.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class FileSystemFolderStoreIntegrationTests : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"altivy-folders-{Guid.NewGuid():N}");

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(root, "vault-0001"));
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(root, true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListChildrenReturnsOnlyImmediateFoldersAndMarkdownNotes()
    {
        var vault = Path.Combine(root, "vault-0001");
        Directory.CreateDirectory(Path.Combine(vault, "projetos", "interno"));
        await File.WriteAllTextAsync(Path.Combine(vault, "raiz.md"), "# Raiz");
        await File.WriteAllTextAsync(Path.Combine(vault, "ignorado.txt"), "texto");
        await File.WriteAllTextAsync(Path.Combine(vault, "projetos", "plano.md"), "# Plano");
        var store = new FileSystemFolderStore(root);

        var rootEntries = await store.ListChildrenAsync("vault-0001", null, CancellationToken.None);
        var childEntries = await store.ListChildrenAsync(
            "vault-0001",
            FolderPath.Create("projetos"),
            CancellationToken.None);

        Assert.Collection(
            rootEntries.OrderBy(entry => entry.Path),
            entry => Assert.Equal(new VaultEntry("projetos", "projetos", true, null), entry),
            entry => Assert.Equal("raiz.md", entry.Path));
        Assert.Collection(
            childEntries.OrderBy(entry => entry.Path),
            entry => Assert.Equal(new VaultEntry("projetos/interno", "interno", true, null), entry),
            entry => Assert.Equal("projetos/plano.md", entry.Path));
    }

    [Fact]
    public async Task EnsureCreatedIsIdempotentAndRequiresAnExistingParent()
    {
        var store = new FileSystemFolderStore(root);

        Assert.Equal(FolderCreationResult.Created, await store.EnsureCreatedAsync(
            "vault-0001",
            FolderPath.Create("projetos"),
            CancellationToken.None));
        Assert.Equal(FolderCreationResult.AlreadyExists, await store.EnsureCreatedAsync(
            "vault-0001",
            FolderPath.Create("projetos"),
            CancellationToken.None));
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => store.EnsureCreatedAsync(
            "vault-0001",
            FolderPath.Create("ausente/filha"),
            CancellationToken.None));
    }

    [Fact]
    public async Task EnsureCreatedReturnsConflictWhenAFileUsesTheFolderPath()
    {
        await File.WriteAllTextAsync(Path.Combine(root, "vault-0001", "colisao"), "conteúdo");
        var store = new FileSystemFolderStore(root);

        var result = await store.EnsureCreatedAsync(
            "vault-0001",
            FolderPath.Create("colisao"),
            CancellationToken.None);

        Assert.Equal(FolderCreationResult.Conflict, result);
    }

    [Fact]
    public async Task DeleteEmptyFolderIsIdempotentAndPreservesItsParent()
    {
        var vault = Path.Combine(root, "vault-0001");
        Directory.CreateDirectory(Path.Combine(vault, "projetos", "rascunhos"));
        var store = new FileSystemFolderStore(root);

        var deleted = await store.DeleteEmptyAsync(
            "vault-0001",
            FolderPath.Create("projetos/rascunhos"),
            CancellationToken.None);
        var repeated = await store.DeleteEmptyAsync(
            "vault-0001",
            FolderPath.Create("projetos/rascunhos"),
            CancellationToken.None);

        Assert.Equal(FolderDeletionResult.Deleted, deleted);
        Assert.Equal(FolderDeletionResult.NotFound, repeated);
        Assert.True(Directory.Exists(Path.Combine(vault, "projetos")));
    }

    [Fact]
    public async Task DeleteEmptyFolderRejectsContentsWithoutRemovingThem()
    {
        var vault = Path.Combine(root, "vault-0001");
        var folder = Path.Combine(vault, "projetos");
        Directory.CreateDirectory(folder);
        var notePath = Path.Combine(folder, "plano.md");
        await File.WriteAllTextAsync(notePath, "# Plano");
        var store = new FileSystemFolderStore(root);

        var result = await store.DeleteEmptyAsync(
            "vault-0001",
            FolderPath.Create("projetos"),
            CancellationToken.None);

        Assert.Equal(FolderDeletionResult.NotEmpty, result);
        Assert.True(File.Exists(notePath));
    }

    [Fact]
    public async Task ListChildrenRejectsSymbolicLinkParent()
    {
        var vault = Path.Combine(root, "vault-0001");
        var external = Path.Combine(root, "external");
        Directory.CreateDirectory(external);
        Directory.CreateSymbolicLink(Path.Combine(vault, "link"), external);
        var store = new FileSystemFolderStore(root);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => store.ListChildrenAsync(
            "vault-0001",
            FolderPath.Create("link"),
            CancellationToken.None));
    }

    [Fact]
    public async Task MoveFolderMovesContentsAndReportsNestedNotes()
    {
        var vault = Path.Combine(root, "vault-0001");
        Directory.CreateDirectory(Path.Combine(vault, "projetos", "interno"));
        Directory.CreateDirectory(Path.Combine(vault, "arquivo"));
        await File.WriteAllTextAsync(Path.Combine(vault, "projetos", "plano.md"), "# Plano");
        await File.WriteAllTextAsync(Path.Combine(vault, "projetos", "interno", "nota.md"), "# Nota");
        await File.WriteAllTextAsync(Path.Combine(vault, "projetos", "anexo.txt"), "texto");
        var store = new FileSystemFolderStore(root);

        var outcome = await store.MoveAsync(
            "vault-0001",
            FolderPath.Create("projetos"),
            FolderPath.Create("arquivo/antigos"),
            CancellationToken.None);

        Assert.Equal(FolderMoveResult.Moved, outcome.Result);
        Assert.False(Directory.Exists(Path.Combine(vault, "projetos")));
        Assert.Equal("# Nota", await File.ReadAllTextAsync(Path.Combine(vault, "arquivo", "antigos", "interno", "nota.md")));
        Assert.True(File.Exists(Path.Combine(vault, "arquivo", "antigos", "anexo.txt")));
        Assert.Equal(
            [("projetos/interno/nota.md", "arquivo/antigos/interno/nota.md"), ("projetos/plano.md", "arquivo/antigos/plano.md")],
            outcome.MovedNotes.OrderBy(pair => pair.From, StringComparer.Ordinal));
    }

    [Fact]
    public async Task MoveFolderNeverOverwritesOrCreatesParents()
    {
        var vault = Path.Combine(root, "vault-0001");
        Directory.CreateDirectory(Path.Combine(vault, "projetos"));
        Directory.CreateDirectory(Path.Combine(vault, "arquivo"));
        var store = new FileSystemFolderStore(root);

        var exists = await store.MoveAsync(
            "vault-0001", FolderPath.Create("projetos"), FolderPath.Create("arquivo"), CancellationToken.None);
        var missingParent = await store.MoveAsync(
            "vault-0001", FolderPath.Create("projetos"), FolderPath.Create("ausente/projetos"), CancellationToken.None);
        var missingSource = await store.MoveAsync(
            "vault-0001", FolderPath.Create("inexistente"), FolderPath.Create("outro"), CancellationToken.None);

        Assert.Equal(FolderMoveResult.DestinationExists, exists.Result);
        Assert.Equal(FolderMoveResult.DestinationFolderNotFound, missingParent.Result);
        Assert.Equal(FolderMoveResult.NotFound, missingSource.Result);
        Assert.True(Directory.Exists(Path.Combine(vault, "projetos")));
    }

    [Fact]
    public async Task MoveFolderRejectsSymbolicLinksInsideTheSubtree()
    {
        var vault = Path.Combine(root, "vault-0001");
        var external = Path.Combine(root, "external");
        Directory.CreateDirectory(external);
        Directory.CreateDirectory(Path.Combine(vault, "projetos"));
        Directory.CreateSymbolicLink(Path.Combine(vault, "projetos", "link"), external);
        var store = new FileSystemFolderStore(root);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => store.MoveAsync(
            "vault-0001", FolderPath.Create("projetos"), FolderPath.Create("movida"), CancellationToken.None));
        Assert.True(Directory.Exists(Path.Combine(vault, "projetos")));
    }
}