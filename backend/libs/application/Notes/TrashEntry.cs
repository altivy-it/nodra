namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Representa uma entrada removida, isolada no inventário de lixeira do sujeito.
/// </summary>
public sealed record TrashEntry(
    Guid Id,
    string OriginalPath,
    string Name,
    bool IsFolder,
    DateTimeOffset DeletedAtUtc,
    int? RetentionDays,
    bool CanRestoreToOriginal);

/// <summary>
/// Resultado da movimentação de uma entrada para a lixeira.
/// </summary>
public sealed record TrashMoveOutcome(
    TrashMoveResult Result,
    TrashEntry? Entry = null,
    IReadOnlyList<string>? NotePaths = null);

/// <summary>
/// Resultados possíveis ao mover uma entrada para a lixeira.
/// </summary>
public enum TrashMoveResult
{
    Moved,
    NotFound,
    VersionConflict,
    UnsupportedEntry
}

/// <summary>
/// Resultado da restauração de uma operação da lixeira.
/// </summary>
public enum TrashRestoreResult
{
    Restored,
    NotFound,
    DestinationExists,
    DestinationFolderNotFound
}

/// <summary>
/// Resultado da restauração e caminhos de nota que exigem reconciliação.
/// </summary>
public sealed record TrashRestoreOutcome(
    TrashRestoreResult Result,
    IReadOnlyList<(string From, string To)>? MovedNotes = null);

/// <summary>
/// Resultado da remoção definitiva da lixeira do sujeito.
/// </summary>
public sealed record TrashEmptyOutcome(int RemovedOperations, int FailedOperations);
