namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Trabalho durável de indexação de um vault.
/// </summary>
public sealed record NoteIndexJob(Guid JobId, string VaultId, string? Path, int AttemptCount, Guid LeaseId);
