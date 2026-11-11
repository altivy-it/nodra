using Nodra.Backend.Domain.Notes;

namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Coordena a consulta dos filhos imediatos de uma pasta do vault.
/// </summary>
public sealed class ListFolderChildrenQueryHandler(IFolderStore folderStore)
{
    /// <summary>
    /// Lista pastas e notas diretamente contidas na pasta informada.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="parentPath">Caminho relativo da pasta; vazio representa a raiz.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Filhos imediatos ordenados pelo caminho relativo.</returns>
    public Task<IReadOnlyList<VaultEntry>> HandleAsync(
        string vaultId,
        string? parentPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vaultId) || vaultId.Length > 64)
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(vaultId));
        }

        var folder = string.IsNullOrWhiteSpace(parentPath) ? null : FolderPath.Create(parentPath);
        return folderStore.ListChildrenAsync(vaultId, folder, cancellationToken);
    }
}
