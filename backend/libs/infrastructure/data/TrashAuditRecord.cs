namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Evento append-only de operação da lixeira.
/// </summary>
public sealed class TrashAuditRecord
{
    public Guid AuditId { get; set; }

    public Guid OperationId { get; set; }

    public string VaultId { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Outcome { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset? ReauthenticatedAtUtc { get; set; }
}
