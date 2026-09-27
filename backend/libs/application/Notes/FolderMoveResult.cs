namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Resultado de mover ou renomear uma pasta do vault.
/// </summary>
public enum FolderMoveResult
{
    /// <summary>A pasta e todo o seu conteúdo foram movidos.</summary>
    Moved,

    /// <summary>A pasta de origem não existe.</summary>
    NotFound,

    /// <summary>Já existe um arquivo ou pasta no caminho de destino.</summary>
    DestinationExists,

    /// <summary>A pasta pai do destino não existe.</summary>
    DestinationFolderNotFound
}

/// <summary>
/// Resultado do movimento com os caminhos das notas Markdown afetadas.
/// </summary>
/// <param name="Result">Resultado do movimento.</param>
/// <param name="MovedNotes">Pares de caminho antigo e novo das notas movidas.</param>
public sealed record FolderMoveOutcome(
    FolderMoveResult Result,
    IReadOnlyList<(string From, string To)> MovedNotes);
