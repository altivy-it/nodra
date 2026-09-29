using Altivy.Backend.Application.Notes;
using Altivy.Backend.Domain.Notes;
using Altivy.Backend.Infra.Data.Postgres;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
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
    public async Task ListChildrenDerivesMetadataIndicatorsWithoutRewritingNotes()
    {
        const string canonicalContent = "---\n" +
            "altivy:\n" +
            "  metadata_version: \"1.0\"\n" +
            "  classification: unclassified\n" +
            "  doc_type: note\n" +
            "  tags: [inbox]\n" +
            "---\n" +
            "# Nova nota\n";
        var vault = Path.Combine(root, "vault-0001");
        var canonicalPath = Path.Combine(vault, "completa.md");
        await File.WriteAllTextAsync(canonicalPath, canonicalContent);
        await File.WriteAllTextAsync(Path.Combine(vault, "externa.md"), "# Nota externa\n");
        var store = new FileSystemFolderStore(root);

        var entries = await store.ListChildrenAsync("vault-0001", null, CancellationToken.None);
        var complete = Assert.Single(entries, entry => entry.Name == "completa.md");
        var external = Assert.Single(entries, entry => entry.Name == "externa.md");

        Assert.True(complete.IsUnclassified);
        Assert.True(complete.HasMinimumMetadata);
        Assert.True(external.IsUnclassified);
        Assert.False(external.HasMinimumMetadata);
        Assert.Equal(canonicalContent, await File.ReadAllTextAsync(canonicalPath));
    }

    [Fact]
    public async Task ListChildrenDerivesUnresolvedLinkIndicatorWithoutRewritingNotes()
    {
        const string canonicalContent = "# Plano\n\nVeja [[Arquitetura]] e [guia](docs/guia.md).\n";
        var vault = Path.Combine(root, "vault-0001");
        var notePath = Path.Combine(vault, "notas", "plano.md");
        Directory.CreateDirectory(Path.Combine(vault, "notas"));
        Directory.CreateDirectory(Path.Combine(vault, "docs"));
        await File.WriteAllTextAsync(notePath, canonicalContent);
        await File.WriteAllTextAsync(Path.Combine(vault, "notas", "Arquitetura.md"), "# Arquitetura\n");
        await File.WriteAllTextAsync(Path.Combine(vault, "docs", "guia.md"), "# Guia\n");
        var store = new FileSystemFolderStore(root);

        var entries = await store.ListChildrenAsync("vault-0001", FolderPath.Create("notas"), CancellationToken.None);
        var note = Assert.Single(entries, entry => entry.Name == "plano.md");

        Assert.False(note.HasUnresolvedLinks);
        Assert.Equal(canonicalContent, await File.ReadAllTextAsync(notePath));

        await File.AppendAllTextAsync(notePath, "\nVeja [[Ausente]].\n");
        entries = await store.ListChildrenAsync("vault-0001", FolderPath.Create("notas"), CancellationToken.None);
        note = Assert.Single(entries, entry => entry.Name == "plano.md");

        Assert.True(note.HasUnresolvedLinks);
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

    [Fact]
    public async Task TrashMovesFolderIntoSubjectInventoryAndRestoresWithoutOverwriting()
    {
        var vault = Path.Combine(root, "vault-0001");
        Directory.CreateDirectory(Path.Combine(vault, "projetos", "interno"));
        Directory.CreateDirectory(Path.Combine(vault, "arquivo", "projetos"));
        await File.WriteAllTextAsync(Path.Combine(vault, "projetos", "plano.md"), "# Plano");
        var store = new FileSystemFolderStore(root);

        var moved = await store.MoveToTrashAsync(
            "vault-0001", "subject-a", FolderPath.Create("projetos"), null, CancellationToken.None);

        Assert.Equal(TrashMoveResult.Moved, moved.Result);
        Assert.NotNull(moved.Entry);
        Assert.False(Directory.Exists(Path.Combine(vault, "projetos")));
        Assert.Single(await store.ListTrashAsync("vault-0001", "subject-a", CancellationToken.None));
        Assert.Empty(await store.ListTrashAsync("vault-0001", "subject-b", CancellationToken.None));
        Assert.Empty(await new FileSystemNoteStore(root).ListAsync("vault-0001", CancellationToken.None));
        Assert.DoesNotContain(await store.ListChildrenAsync("vault-0001", null, CancellationToken.None),
            entry => entry.Path == ".altivy-trash");

        var collision = await store.RestoreFromTrashAsync(
            "vault-0001", "subject-a", moved.Entry.Id, FolderPath.Create("arquivo/projetos"), CancellationToken.None);
        Assert.Equal(TrashRestoreResult.DestinationExists, collision.Result);
        Assert.Single(await store.ListTrashAsync("vault-0001", "subject-a", CancellationToken.None));

        var restored = await store.RestoreFromTrashAsync(
            "vault-0001", "subject-a", moved.Entry.Id, FolderPath.Create("arquivo/restaurado"), CancellationToken.None);

        Assert.Equal(TrashRestoreResult.Restored, restored.Result);
        Assert.Equal([("projetos/plano.md", "arquivo/restaurado/plano.md")], restored.MovedNotes);
        Assert.Equal("# Plano", await File.ReadAllTextAsync(Path.Combine(vault, "arquivo", "restaurado", "plano.md")));
        Assert.Empty(await store.ListTrashAsync("vault-0001", "subject-a", CancellationToken.None));
    }

    [Fact]
    public async Task TrashRejectsAnExistingUnmarkedUserFolderInsteadOfClaimingItsContents()
    {
        var vault = Path.Combine(root, "vault-0001");
        var legacyFolder = Path.Combine(vault, ".altivy-trash");
        Directory.CreateDirectory(legacyFolder);
        await File.WriteAllTextAsync(Path.Combine(legacyFolder, "legado.md"), "# Preservar");
        await File.WriteAllTextAsync(Path.Combine(vault, "nota.md"), "# Nota");
        var store = new FileSystemFolderStore(root);
        var etag = (await new FileSystemNoteStore(root).FindAsync(
            "vault-0001", NotePath.Create("nota.md"), CancellationToken.None))!.ETag;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => store.MoveToTrashAsync(
            "vault-0001", "subject-a", FolderPath.Create("nota.md"), etag, CancellationToken.None));
        Assert.Equal("# Preservar", await File.ReadAllTextAsync(Path.Combine(legacyFolder, "legado.md")));
        Assert.Contains(await store.ListChildrenAsync("vault-0001", null, CancellationToken.None),
            entry => entry.Path == ".altivy-trash");
    }

    [Fact]
    public async Task EmptyTrashDeletesOnlyTheAuthenticatedSubjectsOperations()
    {
        var vault = Path.Combine(root, "vault-0001");
        await File.WriteAllTextAsync(Path.Combine(vault, "nota-a.md"), "A");
        await File.WriteAllTextAsync(Path.Combine(vault, "nota-b.md"), "B");
        var noteStore = new FileSystemNoteStore(root);
        var store = new FileSystemFolderStore(root);
        var noteA = await noteStore.FindAsync("vault-0001", NotePath.Create("nota-a.md"), CancellationToken.None);
        var noteB = await noteStore.FindAsync("vault-0001", NotePath.Create("nota-b.md"), CancellationToken.None);
        var movedA = await store.MoveToTrashAsync(
            "vault-0001", "subject-a", FolderPath.Create("nota-a.md"), noteA!.ETag, CancellationToken.None);
        var movedB = await store.MoveToTrashAsync(
            "vault-0001", "subject-b", FolderPath.Create("nota-b.md"), noteB!.ETag, CancellationToken.None);

        var outcome = await store.EmptyTrashAsync("vault-0001", "subject-a", CancellationToken.None);

        Assert.Equal(new TrashEmptyOutcome(1, 0), outcome);
        Assert.Empty(await store.ListTrashAsync("vault-0001", "subject-a", CancellationToken.None));
        Assert.Single(await store.ListTrashAsync("vault-0001", "subject-b", CancellationToken.None));
        Assert.True(File.Exists(Path.Combine(vault, ".altivy-trash",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("subject-b"))),
            movedB.Entry!.Id.ToString("N"), "payload")));
        Assert.NotNull(movedA.Entry);
    }

    [Fact]
    public async Task EmptyTrashPreservesAnOperationWithAMismatchedManifestSubject()
    {
        await File.WriteAllTextAsync(Path.Combine(root, "vault-0001", "nota.md"), "Nota");
        var note = await new FileSystemNoteStore(root).FindAsync(
            "vault-0001", NotePath.Create("nota.md"), CancellationToken.None);
        var store = new FileSystemFolderStore(root);
        var moved = await store.MoveToTrashAsync(
            "vault-0001", "subject-a", FolderPath.Create("nota.md"), note!.ETag, CancellationToken.None);
        var operationDirectory = Path.Combine(root, "vault-0001", ".altivy-trash",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("subject-a"))),
            moved.Entry!.Id.ToString("N"));
        var manifestPath = Path.Combine(operationDirectory, "manifest.json");
        var manifest = JsonNode.Parse(await File.ReadAllTextAsync(manifestPath))!;
        manifest["Subject"] = "subject-b";
        await File.WriteAllTextAsync(manifestPath, manifest.ToJsonString());

        var outcome = await store.EmptyTrashAsync("vault-0001", "subject-a", CancellationToken.None);

        Assert.Equal(new TrashEmptyOutcome(0, 1), outcome);
        Assert.True(File.Exists(Path.Combine(operationDirectory, "payload")));
    }

    [Fact]
    public async Task TrashManifestCapturesRetentionDaysAtMoveTime()
    {
        await File.WriteAllTextAsync(Path.Combine(root, "vault-0001", "nota.md"), "Nota");
        var note = await new FileSystemNoteStore(root).FindAsync(
            "vault-0001", NotePath.Create("nota.md"), CancellationToken.None);
        var store = new FileSystemFolderStore(root);

        var moved = await store.MoveToTrashAsync(
            "vault-0001", "subject-a", FolderPath.Create("nota.md"), note!.ETag,
            CancellationToken.None, retentionDays: 30);
        var listed = await store.ListTrashAsync("vault-0001", "subject-a", CancellationToken.None);

        Assert.Equal(30, moved.Entry!.RetentionDays);
        Assert.Equal(30, Assert.Single(listed).RetentionDays);
    }
}
