using System.Text;

namespace Nodra.Backend.Infra.Data.Postgres;

internal static class FileSystemTrashArea
{
    internal const string DirectoryName = ".altivy-trash";
    private const string MarkerName = ".altivy-trash-system";
    private const string MarkerContent = "altivy-trash-v1";

    internal static string GetDirectory(string vaultPath) => Path.Combine(vaultPath, DirectoryName);

    internal static bool IsManagedDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        var markerPath = Path.Combine(directoryPath, MarkerName);
        if (!File.Exists(markerPath))
        {
            return false;
        }

        Validate(directoryPath);
        return true;
    }

    internal static bool IsInsideManagedArea(string vaultPath, string candidatePath)
    {
        var relative = Path.GetRelativePath(vaultPath, candidatePath);
        var firstSegment = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return string.Equals(firstSegment, DirectoryName, StringComparison.OrdinalIgnoreCase) &&
            IsManagedDirectory(GetDirectory(vaultPath));
    }

    internal static void EnsureCreated(string vaultPath)
    {
        var directory = GetDirectory(vaultPath);
        if (File.Exists(directory))
        {
            throw new UnauthorizedAccessException("O caminho reservado da lixeira já pertence a um arquivo.");
        }

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            try
            {
                using var marker = new FileStream(Path.Combine(directory, MarkerName), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                marker.Write(Encoding.UTF8.GetBytes(MarkerContent));
            }
            catch
            {
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
                throw;
            }
        }
        else
        {
            Validate(directory);
        }
    }

    internal static void Validate(string directory)
    {
        EnsureNotReparsePoint(directory);
        var markerPath = Path.Combine(directory, MarkerName);
        if (!File.Exists(markerPath))
        {
            throw new UnauthorizedAccessException("O diretório reservado da lixeira já existe sem marcador do sistema.");
        }

        EnsureNotReparsePoint(markerPath);
        if (!string.Equals(File.ReadAllText(markerPath), MarkerContent, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("O marcador da lixeira é inválido.");
        }
    }

    private static void EnsureNotReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new UnauthorizedAccessException("Links simbólicos não são permitidos na lixeira.");
        }
    }
}