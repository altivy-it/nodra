using Nodra.Backend.Domain.Notes;

namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Coordena a exclusão de uma pasta vazia do vault autorizado.
/// </summary>
public sealed class DeleteEmptyFolderCommandHandler(IFolderStore folderStore)
{
    /// <summary>
    /// Exclui somente a pasta informada, sem remover conteúdo recursivamente.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo normalizado da pasta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado da exclusão condicional.</returns>
    public Task<FolderDeletionResult> HandleAsync(
        string vaultId,
        string path,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vaultId) || vaultId.Length > 64)
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(vaultId));
        }

        return folderStore.DeleteEmptyAsync(vaultId, FolderPath.Create(path), cancellationToken);
    }
}