namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Resultado de garantir a existência de uma pasta no vault.
/// </summary>
public enum FolderCreationResult
{
    Created,
    AlreadyExists,
    Conflict
}