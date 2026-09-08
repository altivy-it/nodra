using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Coordena a exclusão condicional de uma nota existente.
/// </summary>
public sealed class DeleteNoteCommandHandler(INoteStore noteStore, INoteIndexJobStore? indexJobStore = null)
{
    /// <summary>
    /// Exclui a nota mediante a versão previamente obtida.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo da nota.</param>
    /// <param name="expectedETag">Versão esperada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado da exclusão condicional.</returns>
    public Task<NoteWriteResult> HandleAsync(
        string vaultId,
        string path,
        string expectedETag,
        CancellationToken cancellationToken) =>
        DeleteAndEnqueueAsync(vaultId, NotePath.Create(path), expectedETag, cancellationToken);

    private async Task<NoteWriteResult> DeleteAndEnqueueAsync(
        string vaultId,
        NotePath path,
        string expectedETag,
        CancellationToken cancellationToken)
    {
        var result = await noteStore.DeleteAsync(vaultId, path, expectedETag, cancellationToken);
        if (result == NoteWriteResult.Updated && indexJobStore is not null)
        {
            await indexJobStore.EnqueueAsync(vaultId, path.Value, $"delete:{vaultId}:{path.Value}:{expectedETag}", cancellationToken);
        }

        return result;
    }
}
