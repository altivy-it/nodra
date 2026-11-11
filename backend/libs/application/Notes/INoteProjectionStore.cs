using Nodra.Backend.Domain.Notes;

namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Define a porta para atualizar a projeção reconstruível de notas.
/// </summary>
public interface INoteProjectionStore
{
    /// <summary>
    /// Insere ou atualiza os metadados da nota de forma idempotente.
    /// </summary>
    Task UpsertAsync(string vaultId, NoteDocument note, CancellationToken cancellationToken);

    /// <summary>
    /// Remove a projeção de uma nota que deixou de existir no filesystem.
    /// </summary>
    Task DeleteAsync(string vaultId, NotePath path, CancellationToken cancellationToken);
}