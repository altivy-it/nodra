using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Define operações de leitura e criação de pastas e entradas imediatas do vault.
/// </summary>
public interface IFolderStore
{
    /// <summary>
    /// Lista somente os filhos diretos de uma pasta autorizada.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="parentPath">Pasta relativa; <see langword="null"/> representa a raiz.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Entradas ordenadas pelo caminho relativo.</returns>
    Task<IReadOnlyList<VaultEntry>> ListChildrenAsync(
        string vaultId,
        FolderPath? parentPath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Garante a existência da pasta sem alterar o estado quando ela já existe.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo e validado da pasta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado da criação ou da colisão com um arquivo.</returns>
    Task<FolderCreationResult> EnsureCreatedAsync(
        string vaultId,
        FolderPath path,
        CancellationToken cancellationToken);

    /// <summary>
    /// Exclui uma pasta somente quando não possui arquivos ou subpastas.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo e validado da pasta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado da exclusão sem remoção recursiva de conteúdo.</returns>
    Task<FolderDeletionResult> DeleteEmptyAsync(
        string vaultId,
        FolderPath path,
        CancellationToken cancellationToken);

    /// <summary>
    /// Move ou renomeia uma pasta e seu conteúdo sem sobrescrever o destino nem criar pastas pai.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="source">Caminho atual da pasta.</param>
    /// <param name="destination">Novo caminho da pasta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado e as notas cujo caminho mudou.</returns>
    Task<FolderMoveOutcome> MoveAsync(
        string vaultId,
        FolderPath source,
        FolderPath destination,
        CancellationToken cancellationToken);
}