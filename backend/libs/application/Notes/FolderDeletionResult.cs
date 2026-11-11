namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Resultado da tentativa de excluir uma pasta sem remover seu conteúdo.
/// </summary>
public enum FolderDeletionResult
{
    Deleted,
    NotFound,
    NotEmpty,
    Conflict
}