namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Esvazia a lixeira depois que a API validou autorização e step-up OIDC recente.
/// </summary>
public sealed class EmptyTrashCommandHandler(ITrashStore trashStore, ITrashAuditStore auditStore)
{
    public async Task<TrashEmptyOutcome> HandleAsync(
        string vaultId,
        string subject,
        DateTimeOffset reauthenticatedAtUtc,
        CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid();
        await auditStore.RecordAsync(new TrashAuditEvent(
            operationId,
            vaultId,
            subject,
            "empty-trash-intent",
            "started",
            DateTimeOffset.UtcNow,
            reauthenticatedAtUtc), cancellationToken);

        try
        {
            var outcome = await trashStore.EmptyTrashAsync(vaultId, subject, cancellationToken);
            await auditStore.RecordAsync(new TrashAuditEvent(
                operationId,
                vaultId,
                subject,
                "empty-trash-result",
                outcome.FailedOperations == 0 ? "succeeded" : "partial-failure",
                DateTimeOffset.UtcNow,
                reauthenticatedAtUtc), cancellationToken);
            return outcome;
        }
        catch
        {
            await auditStore.RecordAsync(new TrashAuditEvent(
                operationId,
                vaultId,
                subject,
                "empty-trash-result",
                "failed",
                DateTimeOffset.UtcNow,
                reauthenticatedAtUtc), cancellationToken);
            throw;
        }
    }
}
