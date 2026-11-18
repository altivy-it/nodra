namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Registra eventos append-only de lixeira sem conteúdo de notas ou paths.
/// </summary>
public interface ITrashAuditStore
{
    Task RecordAsync(TrashAuditEvent auditEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Evento sanitizado de operação de lixeira.
/// </summary>
public sealed record TrashAuditEvent(
    Guid OperationId,
    string VaultId,
    string Subject,
    string Action,
    string Outcome,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? ReauthenticatedAtUtc);
