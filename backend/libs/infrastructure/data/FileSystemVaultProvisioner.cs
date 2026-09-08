using Altivy.Backend.Application.Vaults;
using Altivy.Backend.Domain.Vaults;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Provisiona diretórios de vault sem permitir escape da raiz configurada.
/// </summary>
public sealed class FileSystemVaultProvisioner(string vaultRoot) : IVaultFileSystemProvisioner
{
    private readonly string vaultRoot = Path.GetFullPath(vaultRoot);

    /// <inheritdoc />
    public Task ProvisionAsync(string vaultId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(vaultRoot);
        EnsureNotReparsePoint(vaultRoot);

        var vaultPath = GetVaultPath(vaultId);

        if (Directory.Exists(vaultPath))
        {
            EnsureNotReparsePoint(vaultPath);
            return Task.CompletedTask;
        }

        Directory.CreateDirectory(vaultPath);
        EnsureNotReparsePoint(vaultPath);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string vaultId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var vaultPath = GetVaultPath(vaultId);
        if (!Directory.Exists(vaultRoot))
        {
            return Task.FromResult(false);
        }

        EnsureNotReparsePoint(vaultRoot);
        if (!Directory.Exists(vaultPath))
        {
            return Task.FromResult(false);
        }

        EnsureNotReparsePoint(vaultPath);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListVaultIdsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(vaultRoot))
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        EnsureNotReparsePoint(vaultRoot);
        var vaultIds = new List<string>();
        foreach (var directory in Directory.EnumerateDirectories(vaultRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directoryName = Path.GetFileName(directory);
            if (string.Equals(directoryName, ".quarantine", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                _ = Vault.Create(directoryName, directoryName);
            }
            catch (ArgumentException)
            {
                continue;
            }

            EnsureNotReparsePoint(directory);
            vaultIds.Add(directoryName);
        }

        return Task.FromResult<IReadOnlyList<string>>(vaultIds);
    }

    /// <inheritdoc />
    public Task<bool> QuarantineAsync(string vaultId, DateTimeOffset detectedAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(vaultRoot))
        {
            return Task.FromResult(false);
        }

        EnsureNotReparsePoint(vaultRoot);
        var vaultPath = GetVaultPath(vaultId);
        if (!Directory.Exists(vaultPath))
        {
            return Task.FromResult(false);
        }

        EnsureNotReparsePoint(vaultPath);
        var quarantineRoot = Path.Combine(vaultRoot, ".quarantine");
        Directory.CreateDirectory(quarantineRoot);
        EnsureNotReparsePoint(quarantineRoot);
        var destination = Path.Combine(
            quarantineRoot,
            $"{vaultId}-{detectedAt.UtcDateTime:yyyyMMddHHmmssfffffff}");
        Directory.Move(vaultPath, destination);
        return Task.FromResult(true);
    }

    private string GetVaultPath(string vaultId)
    {
        _ = Vault.Create(vaultId, vaultId);
        var vaultPath = Path.GetFullPath(Path.Combine(vaultRoot, vaultId));
        if (!vaultPath.StartsWith(vaultRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("O diretório do vault está fora da raiz configurada.");
        }

        return vaultPath;
    }

    private static void EnsureNotReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new UnauthorizedAccessException("Links simbólicos não são permitidos no diretório do vault.");
        }
    }
}
