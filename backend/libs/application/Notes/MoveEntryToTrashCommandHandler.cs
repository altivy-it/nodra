using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Move uma nota ou árvore de pastas para a lixeira e reconcilia sua projeção.
/// </summary>
public sealed class MoveEntryToTrashCommandHandler(
    ITrashStore trashStore,
    INoteIndexJobStore? indexJobStore = null,
    ITrashAuditStore? auditStore = null)
{
    /// <summary>
    /// Move a entrada depois de validar o vault, o sujeito e a precondição de nota quando aplicável.
    /// </summary>
    public async Task<TrashMoveOutcome> HandleAsync(
        string vaultId,
        string subject,
        string path,
        string? expectedETag,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vaultId) || vaultId.Length > 64)
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(vaultId));
        }

        var outcome = await trashStore.MoveToTrashAsync(
            vaultId, subject, FolderPath.Create(path), expectedETag, cancellationToken);
        if (auditStore is not null)
        {
            await auditStore.RecordAsync(new TrashAuditEvent(
                outcome.Entry?.Id ?? Guid.NewGuid(),
                vaultId,
                subject,
                "move-to-trash",
                outcome.Result.ToString(),
                DateTimeOffset.UtcNow,
                null), cancellationToken);
        }
        if (outcome.Result == TrashMoveResult.Moved && outcome.Entry is not null && indexJobStore is not null)
        {
            foreach (var notePath in outcome.NotePaths ?? [])
            {
                await indexJobStore.EnqueueAsync(
                    vaultId,
                    notePath,
                    $"trash:{outcome.Entry.Id:N}:remove:{notePath}",
                    cancellationToken);
            }
        }

        return outcome;
    }
}
