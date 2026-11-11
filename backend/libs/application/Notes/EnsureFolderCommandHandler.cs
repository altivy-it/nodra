using Nodra.Backend.Domain.Notes;

namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Garante que uma pasta exista no vault autorizado.
/// </summary>
public sealed class EnsureFolderCommandHandler(IFolderStore folderStore)
{
    /// <summary>
    /// Cria a pasta quando ausente e não altera o estado quando ela já existe.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo normalizado da pasta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado da criação ou da colisão com um arquivo.</returns>
    public Task<FolderCreationResult> HandleAsync(
        string vaultId,
        string path,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vaultId) || vaultId.Length > 64)
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(vaultId));
        }

        return folderStore.EnsureCreatedAsync(vaultId, FolderPath.Create(path), cancellationToken);
    }
}