namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Define o resultado de uma alteração condicional de nota.
/// </summary>
public enum NoteWriteResult
{
    /// <summary>A nota foi alterada.</summary>
    Updated,

    /// <summary>A nota não existe.</summary>
    NotFound,

    /// <summary>A versão esperada diverge da versão atual.</summary>
    Conflict
}
