using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Persiste operações reversíveis da lixeira sem alterar a fonte canônica fora do filesystem.
/// </summary>
public interface ITrashStore
{
    /// <summary>
    /// Move uma nota Markdown ou pasta para a lixeira do sujeito.
    /// </summary>
    Task<TrashMoveOutcome> MoveToTrashAsync(
        string vaultId,
        string subject,
        FolderPath path,
        string? expectedETag,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lista as operações de lixeira pertencentes ao sujeito.
    /// </summary>
    Task<IReadOnlyList<TrashEntry>> ListTrashAsync(
        string vaultId,
        string subject,
        CancellationToken cancellationToken);

    /// <summary>
    /// Restaura uma operação para um destino relativo validado, sem sobrescrever conteúdo.
    /// </summary>
    Task<TrashRestoreOutcome> RestoreFromTrashAsync(
        string vaultId,
        string subject,
        Guid operationId,
        FolderPath destination,
        CancellationToken cancellationToken);

    Task<TrashEmptyOutcome> EmptyTrashAsync(
        string vaultId,
        string subject,
        CancellationToken cancellationToken);
}
