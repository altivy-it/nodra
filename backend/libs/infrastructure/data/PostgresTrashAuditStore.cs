using Altivy.Backend.Application.Notes;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Persiste eventos de lixeira sem atualizar ou excluir eventos existentes.
/// </summary>
public sealed class PostgresTrashAuditStore(PostgresDbContext db) : ITrashAuditStore
{
    public async Task RecordAsync(TrashAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        db.Set<TrashAuditRecord>().Add(new TrashAuditRecord
        {
            AuditId = Guid.NewGuid(),
            OperationId = auditEvent.OperationId,
            VaultId = auditEvent.VaultId,
            Subject = auditEvent.Subject,
            Action = auditEvent.Action,
            Outcome = auditEvent.Outcome,
            OccurredAtUtc = auditEvent.OccurredAtUtc,
            ReauthenticatedAtUtc = auditEvent.ReauthenticatedAtUtc
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
