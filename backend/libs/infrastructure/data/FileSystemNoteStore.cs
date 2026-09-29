using System.Security.Cryptography;
using System.Text;
using Altivy.Backend.Application.Notes;
using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Persiste as notas canônicas em um diretório local montado para os vaults.
/// </summary>
public sealed class FileSystemNoteStore(string vaultRoot) : INoteStore
{
    private readonly string vaultRoot = Path.GetFullPath(vaultRoot);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteDocument>> ListAsync(string vaultId, CancellationToken cancellationToken)
    {
        var root = GetVaultRoot(vaultId);
        if (!Directory.Exists(root))
        {
            return [];
        }

        var documents = new List<NoteDocument>();
        foreach (var filePath in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FileSystemTrashArea.IsInsideManagedArea(root, filePath))
            {
                continue;
            }
            var relativePath = Path.GetRelativePath(root, filePath).Replace('\\', '/');
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            documents.Add(new NoteDocument(NotePath.Create(relativePath), content, CreateETag(content)));
        }

        return documents.OrderBy(document => document.Path.Value, StringComparer.Ordinal).ToArray();
    }

    /// <inheritdoc />
    public async Task<NoteDocument?> FindAsync(string vaultId, NotePath path, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath(vaultId, path);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return new NoteDocument(path, content, CreateETag(content));
    }

    /// <inheritdoc />
    public async Task<bool> TryCreateAsync(string vaultId, NotePath path, string content, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath(vaultId, path);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        try
        {
            await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteAsync(content.AsMemory(), cancellationToken);
            return true;
        }
        catch (IOException) when (File.Exists(filePath))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<NoteWriteResult> UpdateAsync(
        string vaultId, NotePath path, string content, string expectedETag, CancellationToken cancellationToken)
    {
        var existing = await FindAsync(vaultId, path, cancellationToken);
        if (existing is null)
        {
            return NoteWriteResult.NotFound;
        }

        if (!string.Equals(existing.ETag, expectedETag, StringComparison.Ordinal))
        {
            return NoteWriteResult.Conflict;
        }

        var filePath = GetFilePath(vaultId, path);
        var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(temporaryPath, content, new UTF8Encoding(false), cancellationToken);
        File.Move(temporaryPath, filePath, true);
        return NoteWriteResult.Updated;
    }

    /// <inheritdoc />
    public async Task<NoteWriteResult> DeleteAsync(
        string vaultId, NotePath path, string expectedETag, CancellationToken cancellationToken)
    {
        var existing = await FindAsync(vaultId, path, cancellationToken);
        if (existing is null)
        {
            return NoteWriteResult.NotFound;
        }

        if (!string.Equals(existing.ETag, expectedETag, StringComparison.Ordinal))
        {
            return NoteWriteResult.Conflict;
        }

        File.Delete(GetFilePath(vaultId, path));
        return NoteWriteResult.Updated;
    }

    /// <inheritdoc />
    public async Task<NoteMoveResult> MoveAsync(
        string vaultId,
        NotePath source,
        NotePath destination,
        string expectedETag,
        CancellationToken cancellationToken)
    {
        var existing = await FindAsync(vaultId, source, cancellationToken);
        if (existing is null)
        {
            return NoteMoveResult.NotFound;
        }

        if (!string.Equals(existing.ETag, expectedETag, StringComparison.Ordinal))
        {
            return NoteMoveResult.VersionConflict;
        }

        var sourcePath = GetFilePath(vaultId, source);
        var destinationPath = GetFilePath(vaultId, destination);
        if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
        {
            return NoteMoveResult.DestinationFolderNotFound;
        }

        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
        {
            return NoteMoveResult.DestinationExists;
        }

        try
        {
            // overwrite:false keeps the move from replacing a file created concurrently at the destination.
            File.Move(sourcePath, destinationPath, overwrite: false);
        }
        catch (IOException) when (File.Exists(destinationPath))
        {
            return NoteMoveResult.DestinationExists;
        }

        return NoteMoveResult.Moved;
    }

    private string GetVaultRoot(string vaultId)
    {
        var root = Path.GetFullPath(Path.Combine(vaultRoot, vaultId));
        if (!root.StartsWith(vaultRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(vaultId));
        }

        return root;
    }

    private string GetFilePath(string vaultId, NotePath path)
    {
        var vaultPath = GetVaultRoot(vaultId);
        var filePath = Path.GetFullPath(Path.Combine(vaultPath, path.Value));
        if (!filePath.StartsWith(vaultPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("O caminho da nota é inválido.", nameof(path));
        }

        return filePath;
    }

    private static string CreateETag(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
