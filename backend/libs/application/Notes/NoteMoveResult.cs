namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Resultado de mover ou renomear uma nota mediante a versão esperada.
/// </summary>
public enum NoteMoveResult
{
    /// <summary>A nota foi movida para o destino.</summary>
    Moved,

    /// <summary>A nota de origem não existe.</summary>
    NotFound,

    /// <summary>A versão esperada diverge da versão atual da origem.</summary>
    VersionConflict,

    /// <summary>Já existe um arquivo ou pasta no caminho de destino.</summary>
    DestinationExists,

    /// <summary>A pasta de destino não existe.</summary>
    DestinationFolderNotFound
}
