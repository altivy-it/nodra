using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Altivy.Backend.Application.Notes;
using Altivy.Backend.Domain.Notes;
using Altivy.Backend.Domain.Vaults;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Lista filhos imediatos e cria pastas dentro da raiz local de vaults.
/// </summary>
public sealed class FileSystemFolderStore(string vaultRoot) : IFolderStore
{
    private const string TrashDirectoryName = ".altivy-trash";
    private const string ManifestFileName = "manifest.json";
    private const string PendingManifestFileName = "manifest.json.pending";
    private const string PayloadName = "payload";
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
        var knownPaths = Directory.EnumerateFiles(vaultPath, "*.md", SearchOption.AllDirectories)
            .Select(notePath => ToRelativePath(vaultPath, notePath))
            .Where(path => path.IndexOf("/.altivy-trash/", StringComparison.OrdinalIgnoreCase) < 0 &&
                !path.Contains(".altivy-trash", StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in Directory.EnumerateDirectories(parentDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(Path.GetFileName(directory), TrashDirectoryName, StringComparison.OrdinalIgnoreCase) &&
                FileSystemTrashArea.IsManagedDirectory(directory))
            {
                continue;
            }
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
            var relativePath = ToRelativePath(vaultPath, file);
            var metadataStatus = (IsUnclassified: true, HasMinimumMetadata: false);
            bool hasUnresolvedLinks = false;
            try
            {
                var document = MarkdownDocumentParser.Parse(content);
                metadataStatus = (document.IsUnclassified, document.HasMinimumMetadata);
                hasUnresolvedLinks = document.HasUnresolvedLinks(relativePath, knownPaths);
            }
            catch (ArgumentException)
            {
                metadataStatus = (true, false);
                hasUnresolvedLinks = false;
            }

            entries.Add(new VaultEntry(
                relativePath,
                name,
                false,
                CreateETag(content),
                metadataStatus.IsUnclassified,
                metadataStatus.HasMinimumMetadata,
                hasUnresolvedLinks));
        }

        return entries.OrderBy(entry => entry.Path, StringComparer.Ordinal).ToArray();
    }

    /// <inheritdoc />
    public async Task<TrashMoveOutcome> MoveToTrashAsync(
        string vaultId,
        string subject,
        FolderPath path,
        string? expectedETag,
        CancellationToken cancellationToken,
        int? retentionDays = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateSubject(subject);
        ArgumentNullException.ThrowIfNull(path);
        if (retentionDays is < 1 or > 3650)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays));
        }

        var vaultPath = GetVaultPath(vaultId);
        var sourcePath = GetFolderPath(vaultPath, path);
        var isFolder = Directory.Exists(sourcePath);
        if (!isFolder && (!File.Exists(sourcePath) || !sourcePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            return new TrashMoveOutcome(File.Exists(sourcePath) ? TrashMoveResult.UnsupportedEntry : TrashMoveResult.NotFound);
        }

        EnsureNotReparsePoint(sourcePath);
        if (isFolder)
        {
            EnsureSubtreeHasNoReparsePoints(sourcePath, cancellationToken);
        }
        else
        {
            var content = await File.ReadAllTextAsync(sourcePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(expectedETag) ||
                !string.Equals(CreateETag(content), expectedETag, StringComparison.Ordinal))
            {
                return new TrashMoveOutcome(TrashMoveResult.VersionConflict);
            }
        }

        var notePaths = isFolder
            ? Directory.EnumerateFiles(sourcePath, "*.md", SearchOption.AllDirectories)
                .Select(notePath => Path.GetRelativePath(vaultPath, notePath).Replace('\\', '/'))
                .OrderBy(notePath => notePath, StringComparer.Ordinal)
                .ToArray()
            : [path.Value];

        var parentPath = Path.GetDirectoryName(sourcePath)!;
        EnsureNotReparsePoint(parentPath);
        var operationId = Guid.NewGuid();
        var operationDirectory = GetOperationDirectory(vaultPath, subject, operationId, createIfMissing: true);
        Directory.CreateDirectory(operationDirectory);
        EnsureNotReparsePoint(operationDirectory);

        var manifest = new TrashManifest(
            operationId,
            vaultId,
            subject,
            path.Value,
            isFolder,
            DateTimeOffset.UtcNow,
            retentionDays);
        var pendingManifestPath = Path.Combine(operationDirectory, PendingManifestFileName);
        var manifestPath = Path.Combine(operationDirectory, ManifestFileName);
        var payloadPath = Path.Combine(operationDirectory, PayloadName);
        try
        {
            await using (var stream = new FileStream(
                pendingManifestPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, manifest, cancellationToken: cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            if (isFolder)
            {
                Directory.Move(sourcePath, payloadPath);
            }
            else
            {
                File.Move(sourcePath, payloadPath, overwrite: false);
            }
            File.Move(pendingManifestPath, manifestPath, overwrite: false);
        }
        catch
        {
            if (!File.Exists(manifestPath))
            {
                RollbackPendingTrashMove(sourcePath, payloadPath, isFolder, operationDirectory);
            }
            throw;
        }

        return new TrashMoveOutcome(TrashMoveResult.Moved, ToTrashEntry(vaultPath, manifest), notePaths);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrashEntry>> ListTrashAsync(
        string vaultId,
        string subject,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateSubject(subject);
        var vaultPath = GetVaultPath(vaultId);
        var subjectDirectory = GetSubjectTrashDirectory(vaultPath, subject, createIfMissing: false);
        if (!Directory.Exists(subjectDirectory))
        {
            return [];
        }

        EnsureNotReparsePoint(subjectDirectory);
        var entries = new List<TrashEntry>();
        foreach (var operationDirectory in Directory.EnumerateDirectories(subjectDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotReparsePoint(operationDirectory);
            var operationName = Path.GetFileName(operationDirectory);
            if (!Guid.TryParseExact(operationName, "N", out var operationId))
            {
                throw new UnauthorizedAccessException("A área reservada da lixeira contém um identificador inválido.");
            }

            var manifest = await LoadManifestAsync(operationDirectory, vaultId, subject, operationId, cancellationToken);
            if (manifest is null)
            {
                continue;
            }
            ValidateManifest(manifest, vaultId, subject, operationId);
            var payloadPath = Path.Combine(operationDirectory, PayloadName);
            var payloadExists = manifest.IsFolder ? Directory.Exists(payloadPath) : File.Exists(payloadPath);
            if (!payloadExists)
            {
                TryDeleteOperationDirectory(operationDirectory);
                continue;
            }
            EnsureNotReparsePoint(payloadPath);
            if (manifest.IsFolder)
            {
                EnsureSubtreeHasNoReparsePoints(payloadPath, cancellationToken);
            }
            entries.Add(ToTrashEntry(vaultPath, manifest));
        }

        return entries.OrderByDescending(entry => entry.DeletedAtUtc).ThenBy(entry => entry.Id).ToArray();
    }

    /// <inheritdoc />
    public async Task<TrashRestoreOutcome> RestoreFromTrashAsync(
        string vaultId,
        string subject,
        Guid operationId,
        FolderPath destination,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateSubject(subject);
        ArgumentNullException.ThrowIfNull(destination);
        var vaultPath = GetVaultPath(vaultId);
        var operationDirectory = GetOperationDirectory(vaultPath, subject, operationId, createIfMissing: false);
        if (!Directory.Exists(operationDirectory))
        {
            return new TrashRestoreOutcome(TrashRestoreResult.NotFound);
        }

        EnsureNotReparsePoint(operationDirectory);
        var manifest = await LoadManifestAsync(operationDirectory, vaultId, subject, operationId, cancellationToken);
        if (manifest is null)
        {
            return new TrashRestoreOutcome(TrashRestoreResult.NotFound);
        }
        ValidateManifest(manifest, vaultId, subject, operationId);

        var destinationPath = GetFolderPath(vaultPath, destination);
        if (!manifest.IsFolder)
        {
            _ = NotePath.Create(destination.Value);
        }
        var destinationParent = Path.GetDirectoryName(destinationPath)!;
        if (!Directory.Exists(destinationParent))
        {
            return new TrashRestoreOutcome(TrashRestoreResult.DestinationFolderNotFound);
        }
        EnsureNotReparsePoint(destinationParent);
        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
        {
            return new TrashRestoreOutcome(TrashRestoreResult.DestinationExists);
        }

        var payloadPath = Path.Combine(operationDirectory, PayloadName);
        var payloadExists = manifest.IsFolder ? Directory.Exists(payloadPath) : File.Exists(payloadPath);
        if (!payloadExists)
        {
            TryDeleteOperationDirectory(operationDirectory);
            return new TrashRestoreOutcome(TrashRestoreResult.NotFound);
        }
        EnsureNotReparsePoint(payloadPath);
        var movedNotes = new List<(string From, string To)>();
        if (manifest.IsFolder)
        {
            EnsureSubtreeHasNoReparsePoints(payloadPath, cancellationToken);
            foreach (var notePath in Directory.EnumerateFiles(payloadPath, "*.md", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = Path.GetRelativePath(payloadPath, notePath).Replace('\\', '/');
                movedNotes.Add(($"{manifest.OriginalPath}/{relativePath}", $"{destination.Value}/{relativePath}"));
            }
            Directory.Move(payloadPath, destinationPath);
        }
        else
        {
            File.Move(payloadPath, destinationPath, overwrite: false);
            movedNotes.Add((manifest.OriginalPath, destination.Value));
        }
        TryDeleteOperationDirectory(operationDirectory);
        return new TrashRestoreOutcome(TrashRestoreResult.Restored, movedNotes);
    }

    /// <inheritdoc />
    public async Task<TrashEmptyOutcome> EmptyTrashAsync(
        string vaultId,
        string subject,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateSubject(subject);
        var vaultPath = GetVaultPath(vaultId);
        var subjectDirectory = GetSubjectTrashDirectory(vaultPath, subject, createIfMissing: false);
        if (!Directory.Exists(subjectDirectory)) return new TrashEmptyOutcome(0, 0);

        EnsureNotReparsePoint(subjectDirectory);
        var removed = 0;
        var failed = 0;
        foreach (var operationDirectory in Directory.EnumerateDirectories(subjectDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                EnsureNotReparsePoint(operationDirectory);
                var operationName = Path.GetFileName(operationDirectory);
                if (!Guid.TryParseExact(operationName, "N", out var operationId))
                {
                    throw new UnauthorizedAccessException("A operação da lixeira possui um identificador inválido.");
                }
                var manifest = await LoadManifestAsync(
                    operationDirectory, vaultId, subject, operationId, cancellationToken);
                if (manifest is null) continue;
                ValidateManifest(manifest, vaultId, subject, operationId);
                TryDeleteOperationDirectory(operationDirectory);
                removed++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (IOException)
            {
                failed++;
            }
            catch (UnauthorizedAccessException)
            {
                failed++;
            }
        }

        return new TrashEmptyOutcome(removed, failed);
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

    private static void ValidateSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 512)
        {
            throw new ArgumentException("O sujeito autenticado é inválido.", nameof(subject));
        }
    }

    private static TrashEntry ToTrashEntry(string vaultPath, TrashManifest manifest)
    {
        var original = GetFolderPath(vaultPath, FolderPath.Create(manifest.OriginalPath));
        var parent = Path.GetDirectoryName(original)!;
        var canRestore = Directory.Exists(parent) && !Directory.Exists(original) && !File.Exists(original);
        return new TrashEntry(
            manifest.Id,
            manifest.OriginalPath,
            manifest.OriginalPath.Split('/').Last(),
            manifest.IsFolder,
            manifest.DeletedAtUtc,
            manifest.RetentionDays,
            canRestore);
    }

    private static void ValidateManifest(TrashManifest manifest, string vaultId, string subject, Guid operationId)
    {
        if (manifest.Version != 1 || manifest.Id != operationId ||
            !string.Equals(manifest.VaultId, vaultId, StringComparison.Ordinal) ||
            !string.Equals(manifest.Subject, subject, StringComparison.Ordinal) ||
            manifest.DeletedAtUtc.Offset != TimeSpan.Zero ||
            manifest.RetentionDays is < 1 or > 3650)
        {
            throw new UnauthorizedAccessException("O manifesto da lixeira é inválido.");
        }

        try
        {
            var path = FolderPath.Create(manifest.OriginalPath);
            if (!manifest.IsFolder)
            {
                _ = NotePath.Create(path.Value);
            }
        }
        catch (ArgumentException exception)
        {
            throw new UnauthorizedAccessException("O caminho do manifesto da lixeira é inválido.", exception);
        }
    }

    private static async Task<TrashManifest?> LoadManifestAsync(
        string operationDirectory,
        string vaultId,
        string subject,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(operationDirectory, ManifestFileName);
        var pendingPath = Path.Combine(operationDirectory, PendingManifestFileName);
        if (File.Exists(manifestPath))
        {
            return await DeserializeManifestAsync(manifestPath, cancellationToken);
        }

        if (File.Exists(pendingPath))
        {
            var pending = await DeserializeManifestAsync(pendingPath, cancellationToken);
            ValidateManifest(pending, vaultId, subject, operationId);
            var pendingPayload = Path.Combine(operationDirectory, PayloadName);
            if (pending.IsFolder ? Directory.Exists(pendingPayload) : File.Exists(pendingPayload))
            {
                File.Move(pendingPath, manifestPath, overwrite: false);
                return pending;
            }

            TryDeleteOperationDirectory(operationDirectory);
            return null;
        }

        if (!Directory.EnumerateFileSystemEntries(operationDirectory).Any())
        {
            TryDeleteOperationDirectory(operationDirectory);
            return null;
        }

        throw new UnauthorizedAccessException("A operação da lixeira não possui manifesto válido.");
    }

    private static async Task<TrashManifest> DeserializeManifestAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await JsonSerializer.DeserializeAsync<TrashManifest>(stream, cancellationToken: cancellationToken)
                ?? throw new UnauthorizedAccessException("O manifesto da lixeira está vazio.");
        }
        catch (JsonException exception)
        {
            throw new UnauthorizedAccessException("O manifesto da lixeira é inválido.", exception);
        }
    }

    private static string GetSubjectTrashDirectory(string vaultPath, string subject, bool createIfMissing)
    {
        var trashRoot = FileSystemTrashArea.GetDirectory(vaultPath);
        if (createIfMissing)
        {
            FileSystemTrashArea.EnsureCreated(vaultPath);
        }
        else if (Directory.Exists(trashRoot))
        {
            FileSystemTrashArea.Validate(trashRoot);
        }

        var subjectHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(subject)));
        var subjectDirectory = Path.Combine(trashRoot, subjectHash);
        if (File.Exists(subjectDirectory))
        {
            throw new UnauthorizedAccessException("O diretório isolado da lixeira é inválido.");
        }
        if (createIfMissing) Directory.CreateDirectory(subjectDirectory);
        if (Directory.Exists(subjectDirectory)) EnsureNotReparsePoint(subjectDirectory);
        return subjectDirectory;
    }

    private static string GetOperationDirectory(string vaultPath, string subject, Guid operationId, bool createIfMissing)
    {
        var subjectDirectory = GetSubjectTrashDirectory(vaultPath, subject, createIfMissing);
        return Path.Combine(subjectDirectory, operationId.ToString("N"));
    }

    private static void EnsureSubtreeHasNoReparsePoints(string root, CancellationToken cancellationToken)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNotReparsePoint(directory);
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                EnsureNotReparsePoint(entry);
                if (Directory.Exists(entry))
                {
                    pending.Push(entry);
                }
            }
        }
    }

    private static void RollbackPendingTrashMove(
        string sourcePath,
        string payloadPath,
        bool isFolder,
        string operationDirectory)
    {
        var payloadExists = isFolder ? Directory.Exists(payloadPath) : File.Exists(payloadPath);
        if (payloadExists && !File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            var parent = Path.GetDirectoryName(sourcePath)!;
            EnsureNotReparsePoint(parent);
            if (isFolder)
            {
                Directory.Move(payloadPath, sourcePath);
            }
            else
            {
                File.Move(payloadPath, sourcePath, overwrite: false);
            }
        }

        if (!Directory.Exists(payloadPath) && !File.Exists(payloadPath))
        {
            TryDeleteOperationDirectory(operationDirectory);
        }
    }

    private static void TryDeleteOperationDirectory(string operationDirectory)
    {
        if (!Directory.Exists(operationDirectory))
        {
            return;
        }

        EnsureSubtreeHasNoReparsePoints(operationDirectory, CancellationToken.None);
        Directory.Delete(operationDirectory, recursive: true);
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

    private sealed record TrashManifest(
        Guid Id,
        string VaultId,
        string Subject,
        string OriginalPath,
        bool IsFolder,
        DateTimeOffset DeletedAtUtc,
        int? RetentionDays,
        int Version = 1);
}
