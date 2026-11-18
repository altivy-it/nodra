namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Coordena a listagem de notas autorizadas pela camada de entrada.
/// </summary>
public sealed class ListNotesQueryHandler(INoteStore noteStore)
{
    /// <summary>
    /// Lista as notas disponíveis no vault.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Notas ordenadas pelo caminho relativo.</returns>
    public Task<IReadOnlyList<NoteDocument>> HandleAsync(string vaultId, CancellationToken cancellationToken) =>
        noteStore.ListAsync(vaultId, cancellationToken);
}
