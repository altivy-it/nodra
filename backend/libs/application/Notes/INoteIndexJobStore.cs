namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Porta da fila PostgreSQL de indexação de notas.
/// </summary>
public interface INoteIndexJobStore
{
    Task EnqueueAsync(string vaultId, string? path, string idempotencyKey, CancellationToken cancellationToken);

    Task<NoteIndexJob?> ClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task<bool> CompleteAsync(
        NoteIndexJob job,
        DateTimeOffset? nextAttemptAt,
        string? lastError,
        CancellationToken cancellationToken);
}