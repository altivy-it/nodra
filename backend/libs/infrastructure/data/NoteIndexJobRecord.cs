namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Registro durável de um trabalho de indexação.
/// </summary>
public sealed class NoteIndexJobRecord
{
    public Guid JobId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string VaultId { get; set; } = string.Empty;

    public string? Path { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public int State { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? NextAttemptAt { get; set; }

    public string? LastError { get; set; }

    public Guid? LeaseId { get; set; }

    public DateTimeOffset? LeaseExpiresAt { get; set; }
}
