using Nodra.Backend.Domain.Notes;

namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Restaura uma operação da lixeira para um destino relativo explícito.
/// </summary>
public sealed class RestoreTrashEntryCommandHandler(
    ITrashStore trashStore,
    INoteIndexJobStore? indexJobStore = null,
    ITrashAuditStore? auditStore = null)
{
    /// <summary>
    /// Restaura uma operação sem sobrescrever destinos e reconcilia as notas movidas.
    /// </summary>
    public async Task<TrashRestoreOutcome> HandleAsync(
        string vaultId,
        string subject,
        Guid operationId,
        string destination,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vaultId) || vaultId.Length > 64)
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(vaultId));
        }
        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("O identificador da operação é inválido.", nameof(operationId));
        }

        var outcome = await trashStore.RestoreFromTrashAsync(
            vaultId, subject, operationId, FolderPath.Create(destination), cancellationToken);
        if (auditStore is not null)
        {
            await auditStore.RecordAsync(new TrashAuditEvent(
                operationId,
                vaultId,
                subject,
                "restore-from-trash",
                outcome.Result.ToString(),
                DateTimeOffset.UtcNow,
                null), cancellationToken);
        }
        if (outcome.Result == TrashRestoreResult.Restored && indexJobStore is not null)
        {
            foreach (var (from, to) in outcome.MovedNotes ?? [])
            {
                await indexJobStore.EnqueueAsync(
                    vaultId, from, $"trash:{operationId:N}:restore-source:{from}", cancellationToken);
                await indexJobStore.EnqueueAsync(
                    vaultId, to, $"trash:{operationId:N}:restore-destination:{to}", cancellationToken);
            }
        }

        return outcome;
    }
}
