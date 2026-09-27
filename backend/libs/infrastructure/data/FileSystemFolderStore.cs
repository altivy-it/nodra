using System.Security.Cryptography;
using System.Text;
using Altivy.Backend.Application.Notes;
using Altivy.Backend.Domain.Notes;
using Altivy.Backend.Domain.Vaults;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Lista filhos imediatos e cria pastas dentro da raiz local de vaults.
/// </summary>
public sealed class FileSystemFolderStore(string vaultRoot) : IFolderStore
{
    private readonly string vaultRoot = Path.GetFullPath(vaultRoot);

    /// <inheritdoc />
    public async Task<IReadOnlyList<VaultEntry>> ListChildrenAsync(
        string vaultId,
        FolderPath? parentPath,
        CancellationToken cancellationToken)
    {
        var vaultPath = GetVaultPath(vaultId);
        if (!Directory.Exists(vaultPath))
        {
            return [];
        }

        var parentDirectory = GetFolderPath(vaultPath, parentPath);
        if (!Directory.Exists(parentDirectory))
        {
            return [];
        }

        EnsureNotReparsePoint(parentDirectory);
        var entries = new List<VaultEntry>();
        foreach (var directory in Directory.EnumerateDirectories(parentDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotReparsePoint(directory);
            var name = Path.GetFileName(directory);
            entries.Add(new VaultEntry(ToRelativePath(vaultPath, directory), name, true, null));
        }

        foreach (var file in Directory.EnumerateFiles(parentDirectory, "*.md", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotReparsePoint(file);
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            var name = Path.GetFileName(file);
            entries.Add(new VaultEntry(ToRelativePath(vaultPath, file), name, false, CreateETag(content)));
        }

        return entries.OrderBy(entry => entry.Path, StringComparer.Ordinal).ToArray();
    }

    /// <inheritdoc />
    public Task<FolderCreationResult> EnsureCreatedAsync(
        string vaultId,
        FolderPath path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(path);

        var vaultPath = GetVaultPath(vaultId);
        if (!Directory.Exists(vaultPath))
        {
            throw new DirectoryNotFoundException("O vault não está provisionado.");
        }

        var destination = GetFolderPath(vaultPath, path);
        var parent = Path.GetDirectoryName(destination)!;
        if (!Directory.Exists(parent))
        {
            throw new DirectoryNotFoundException("A pasta pai não existe.");
        }

        EnsureNotReparsePoint(parent);
        if (Directory.Exists(destination))
        {
            return Task.FromResult(FolderCreationResult.AlreadyExists);
        }

        if (File.Exists(destination))
        {
            return Task.FromResult(FolderCreationResult.Conflict);
        }

        try
        {
            Directory.CreateDirectory(destination);
        }
        catch (IOException) when (Directory.Exists(destination))
        {
            return Task.FromResult(FolderCreationResult.AlreadyExists);
        }
        catch (IOException) when (File.Exists(destination))
        {
            return Task.FromResult(FolderCreationResult.Conflict);
        }

        _ = GetFolderPath(vaultPath, path);
        return Task.FromResult(FolderCreationResult.Created);
    }

    /// <inheritdoc />
    public Task<FolderDeletionResult> DeleteEmptyAsync(
        string vaultId,
        FolderPath path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(path);

        var vaultPath = GetVaultPath(vaultId);
        var folderPath = GetFolderPath(vaultPath, path);
        if (File.Exists(folderPath))
        {
            return Task.FromResult(FolderDeletionResult.Conflict);
        }

        if (!Directory.Exists(folderPath))
        {
            return Task.FromResult(FolderDeletionResult.NotFound);
        }

        EnsureNotReparsePoint(folderPath);
        if (Directory.EnumerateFileSystemEntries(folderPath).Any())
        {
            return Task.FromResult(FolderDeletionResult.NotEmpty);
        }

        EnsureNotReparsePoint(folderPath);
        try
        {
            Directory.Delete(folderPath);
            return Task.FromResult(FolderDeletionResult.Deleted);
        }
        catch (IOException) when (Directory.Exists(folderPath) && Directory.EnumerateFileSystemEntries(folderPath).Any())
        {
            return Task.FromResult(FolderDeletionResult.NotEmpty);
        }
    }

    /// <inheritdoc />
    public Task<FolderMoveOutcome> MoveAsync(
        string vaultId,
        FolderPath source,
        FolderPath destination,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var vaultPath = GetVaultPath(vaultId);
        var sourcePath = GetFolderPath(vaultPath, source);
        var destinationPath = GetFolderPath(vaultPath, destination);
        if (!Directory.Exists(sourcePath))
        {
            return Task.FromResult(new FolderMoveOutcome(FolderMoveResult.NotFound, []));
        }

        EnsureNotReparsePoint(sourcePath);
        if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
        {
            return Task.FromResult(new FolderMoveOutcome(FolderMoveResult.DestinationFolderNotFound, []));
        }

        if (Directory.Exists(destinationPath) || File.Exists(destinationPath))
        {
            return Task.FromResult(new FolderMoveOutcome(FolderMoveResult.DestinationExists, []));
        }

        var movedNotes = new List<(string From, string To)>();
        foreach (var entry in Directory.EnumerateFileSystemEntries(sourcePath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotReparsePoint(entry);
            if (File.Exists(entry) && entry.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var relativeToFolder = Path.GetRelativePath(sourcePath, entry).Replace('\\', '/');
                movedNotes.Add(($"{source.Value}/{relativeToFolder}", $"{destination.Value}/{relativeToFolder}"));
            }
        }

        try
        {
            Directory.Move(sourcePath, destinationPath);
        }
        catch (IOException) when (Directory.Exists(destinationPath) || File.Exists(destinationPath))
        {
            return Task.FromResult(new FolderMoveOutcome(FolderMoveResult.DestinationExists, []));
        }

        return Task.FromResult(new FolderMoveOutcome(FolderMoveResult.Moved, movedNotes));
    }

    private string GetVaultPath(string vaultId)
    {
        _ = Vault.Create(vaultId, vaultId);
        EnsureNotReparsePointIfPresent(vaultRoot);
        var vaultPath = Path.GetFullPath(Path.Combine(vaultRoot, vaultId));
        if (!IsWithin(vaultRoot, vaultPath))
        {
            throw new UnauthorizedAccessException("O diretório do vault está fora da raiz configurada.");
        }

        EnsureNotReparsePointIfPresent(vaultPath);
        return vaultPath;
    }

    private static string GetFolderPath(string vaultPath, FolderPath? folderPath)
    {
        if (folderPath is null)
        {
            return vaultPath;
        }

        var fullPath = Path.GetFullPath(Path.Combine(vaultPath, folderPath.Value.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsWithin(vaultPath, fullPath))
        {
            throw new UnauthorizedAccessException("O caminho da pasta está fora do vault.");
        }

        var currentPath = vaultPath;
        EnsureNotReparsePointIfPresent(currentPath);
        foreach (var segment in folderPath.Value.Split('/'))
        {
            currentPath = Path.Combine(currentPath, segment);
            EnsureNotReparsePointIfPresent(currentPath);
            if (!Directory.Exists(currentPath))
            {
                break;
            }
        }

        return fullPath;
    }

    private static bool IsWithin(string root, string path)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(root, path, comparison) ||
            path.StartsWith(root + Path.DirectorySeparatorChar, comparison);
    }

    private static void EnsureNotReparsePointIfPresent(string path)
    {
        if (Directory.Exists(path) || File.Exists(path))
        {
            EnsureNotReparsePoint(path);
        }
    }

    private static void EnsureNotReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new UnauthorizedAccessException("Links simbólicos não são permitidos no vault.");
        }
    }

    private static string ToRelativePath(string vaultPath, string path) =>
        Path.GetRelativePath(vaultPath, path).Replace('\\', '/');

    private static string CreateETag(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}