using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Coordena a consulta de uma nota autorizada pela camada de entrada.
/// </summary>
public sealed class GetNoteQueryHandler(INoteStore noteStore)
{
    /// <summary>
    /// Localiza a nota pelo caminho relativo.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo da nota.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A nota quando existir; caso contrário, <see langword="null"/>.</returns>
    public Task<NoteDocument?> HandleAsync(string vaultId, string path, CancellationToken cancellationToken) =>
        noteStore.FindAsync(vaultId, NotePath.Create(path), cancellationToken);
}
