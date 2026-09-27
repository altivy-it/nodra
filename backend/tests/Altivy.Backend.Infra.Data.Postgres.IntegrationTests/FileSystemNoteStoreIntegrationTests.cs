using Altivy.Backend.Application.Notes;
using Altivy.Backend.Domain.Notes;
using Altivy.Backend.Infra.Data.Postgres;
using System.Diagnostics;
using Xunit;

namespace Altivy.Backend.Infra.Data.Postgres.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class FileSystemNoteStoreIntegrationTests : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"altivy-notes-{Guid.NewGuid():N}");

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(root);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            {
                Directory.Delete(directory);
            }
        }

        Directory.Delete(root, true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CrudPersistsWithinVaultAndHonorsEtag()
    {
        var store = new FileSystemNoteStore(root);
        var path = NotePath.Create("projetos/plano.md");

        var created = await store.TryCreateAsync("lab-0001", path, "# Inicial", CancellationToken.None);
        var loaded = await store.FindAsync("lab-0001", path, CancellationToken.None);
        var conflict = await store.UpdateAsync("lab-0001", path, "# Ignorado", "desatualizado", CancellationToken.None);
        var updated = await store.UpdateAsync("lab-0001", path, "# Atualizado", loaded!.ETag, CancellationToken.None);
        var current = await store.FindAsync("lab-0001", path, CancellationToken.None);
        var deleted = await store.DeleteAsync("lab-0001", path, current!.ETag, CancellationToken.None);

        Assert.True(created);
        Assert.NotNull(loaded);
        Assert.Equal(NoteWriteResult.Conflict, conflict);
        Assert.Equal(NoteWriteResult.Updated, updated);
        Assert.Equal("# Atualizado", current.Content);
        Assert.Equal(NoteWriteResult.Updated, deleted);
        Assert.Null(await store.FindAsync("lab-0001", path, CancellationToken.None));
    }

    [Fact]
    public async Task MovePreservesContentAndRejectsStaleVersionOrOccupiedDestination()
    {
        var store = new FileSystemNoteStore(root);
        var source = NotePath.Create("rascunho.md");
        var destination = NotePath.Create("projetos/plano.md");
        Directory.CreateDirectory(Path.Combine(root, "lab-0001", "projetos"));
        await store.TryCreateAsync("lab-0001", source, "# Rascunho", CancellationToken.None);
        await store.TryCreateAsync("lab-0001", NotePath.Create("ocupado.md"), "# Outra", CancellationToken.None);
        var current = await store.FindAsync("lab-0001", source, CancellationToken.None);

        var stale = await store.MoveAsync("lab-0001", source, destination, "desatualizado", CancellationToken.None);
        var occupied = await store.MoveAsync(
            "lab-0001", source, NotePath.Create("ocupado.md"), current!.ETag, CancellationToken.None);
        var missingFolder = await store.MoveAsync(
            "lab-0001", source, NotePath.Create("ausente/plano.md"), current.ETag, CancellationToken.None);
        var moved = await store.MoveAsync("lab-0001", source, destination, current.ETag, CancellationToken.None);

        Assert.Equal(NoteMoveResult.VersionConflict, stale);
        Assert.Equal(NoteMoveResult.DestinationExists, occupied);
        Assert.Equal(NoteMoveResult.DestinationFolderNotFound, missingFolder);
        Assert.Equal(NoteMoveResult.Moved, moved);
        Assert.Null(await store.FindAsync("lab-0001", source, CancellationToken.None));
        var movedNote = await store.FindAsync("lab-0001", destination, CancellationToken.None);
        Assert.Equal("# Rascunho", movedNote!.Content);
        Assert.Equal(current.ETag, movedNote.ETag);
        Assert.Equal("# Outra", (await store.FindAsync("lab-0001", NotePath.Create("ocupado.md"), CancellationToken.None))!.Content);
    }

    [Fact]
    public async Task ProvisionCreatesVaultDirectoryIdempotently()
    {
        var provisioner = new FileSystemVaultProvisioner(root);

        await provisioner.ProvisionAsync("vault-0001", CancellationToken.None);
        await provisioner.ProvisionAsync("vault-0001", CancellationToken.None);

        Assert.True(Directory.Exists(Path.Combine(root, "vault-0001")));
    }

    [Fact]
    public async Task ExistsAsyncReturnsWhetherTheTrustedVaultDirectoryExists()
    {
        var provisioner = new FileSystemVaultProvisioner(root);

        Assert.False(await provisioner.ExistsAsync("vault-missing", CancellationToken.None));
        await provisioner.ProvisionAsync("vault-0001", CancellationToken.None);

        Assert.True(await provisioner.ExistsAsync("vault-0001", CancellationToken.None));
    }

    [Fact]
    public async Task QuarantineMovesOnlyValidVaultDirectoryUnderConfinedQuarantineRoot()
    {
        const string orphanVaultId = "vault-orphan";
        Directory.CreateDirectory(Path.Combine(root, orphanVaultId));
        Directory.CreateDirectory(Path.Combine(root, "not a vault"));
        Directory.CreateDirectory(Path.Combine(root, ".quarantine"));
        var provisioner = new FileSystemVaultProvisioner(root);

        var vaultIds = await provisioner.ListVaultIdsAsync(CancellationToken.None);
        var quarantined = await provisioner.QuarantineAsync(
            orphanVaultId,
            DateTimeOffset.UnixEpoch,
            CancellationToken.None);

        Assert.Equal([orphanVaultId], vaultIds);
        Assert.True(quarantined);
        Assert.False(Directory.Exists(Path.Combine(root, orphanVaultId)));
        Assert.Single(Directory.EnumerateDirectories(Path.Combine(root, ".quarantine")));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("C:\\outside")]
    public async Task ProvisionRejectsInvalidVaultIdentifier(string vaultId)
    {
        var provisioner = new FileSystemVaultProvisioner(root);

        await Assert.ThrowsAsync<ArgumentException>(
            () => provisioner.ProvisionAsync(vaultId, CancellationToken.None));
    }

    [Fact]
    public async Task ProvisionRejectsVaultDirectorySymbolicLink()
    {
        const string vaultId = "vault-0002";
        var target = Path.Combine(root, "target");
        Directory.CreateDirectory(target);
        CreateDirectoryReparsePoint(Path.Combine(root, vaultId), target);
        var provisioner = new FileSystemVaultProvisioner(root);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => provisioner.ProvisionAsync(vaultId, CancellationToken.None));
    }

    [Fact]
    public async Task ProvisionRejectsConfiguredRootReparsePoint()
    {
        var target = Path.Combine(root, "target-root");
        var configuredRoot = Path.Combine(root, "vaults");
        Directory.CreateDirectory(target);
        CreateDirectoryReparsePoint(configuredRoot, target);
        var provisioner = new FileSystemVaultProvisioner(configuredRoot);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => provisioner.ProvisionAsync("vault-0003", CancellationToken.None));
    }

    private static void CreateDirectoryReparsePoint(string path, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(path, target);
            return;
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{path}\" \"{target}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        });
        process!.WaitForExit();
        Assert.Equal(0, process.ExitCode);
    }
}
