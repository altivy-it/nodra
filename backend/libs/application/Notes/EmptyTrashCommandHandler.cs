namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Esvazia a lixeira depois que a API validou autorização e step-up OIDC recente.
/// </summary>
public sealed class EmptyTrashCommandHandler(ITrashStore trashStore, ITrashAuditStore? auditStore = null)
{
    public async Task<TrashEmptyOutcome> HandleAsync(
        string vaultId,
        string subject,
        DateTimeOffset reauthenticatedAtUtc,
        CancellationToken cancellationToken)
    {
        if (auditStore is null)
        {
            throw new InvalidOperationException("A auditoria do esvaziamento da lixeira está indisponível.");
        }

        var operationId = Guid.NewGuid();
        await auditStore.RecordAsync(new TrashAuditEvent(
            operationId,
            vaultId,
            subject,
            "empty-trash-intent",
            "started",
            DateTimeOffset.UtcNow,
            reauthenticatedAtUtc), cancellationToken);

        TrashEmptyOutcome outcome;
        try
        {
            outcome = await trashStore.EmptyTrashAsync(vaultId, subject, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await auditStore.RecordAsync(new TrashAuditEvent(
                operationId,
                vaultId,
                subject,
                "empty-trash-result",
                "cancelled",
                DateTimeOffset.UtcNow,
                reauthenticatedAtUtc), CancellationToken.None);
            throw;
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
                reauthenticatedAtUtc), CancellationToken.None);
            throw;
        }

        await auditStore.RecordAsync(new TrashAuditEvent(
            operationId,
            vaultId,
            subject,
            "empty-trash-result",
            outcome.FailedOperations == 0 ? "succeeded" : "partial-failure",
            DateTimeOffset.UtcNow,
            reauthenticatedAtUtc), CancellationToken.None);
        return outcome;
    }
}
