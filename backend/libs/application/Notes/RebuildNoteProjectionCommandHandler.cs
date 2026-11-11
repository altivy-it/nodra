namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Reconstrói a projeção de metadados usando o filesystem como fonte canônica.
/// </summary>
public sealed class RebuildNoteProjectionCommandHandler(
    INoteStore noteStore,
    INoteProjectionStore projectionStore)
{
    /// <summary>
    /// Projeta novamente todas as notas atuais do vault.
    /// </summary>
    public async Task<int> HandleAsync(string vaultId, CancellationToken cancellationToken)
    {
        var notes = await noteStore.ListAsync(vaultId, cancellationToken);
        foreach (var note in notes)
        {
            await projectionStore.UpsertAsync(vaultId, note, cancellationToken);
        }

        return notes.Count;
    }
}
