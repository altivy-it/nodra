using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Coordena a alteração condicional de uma nota existente.
/// </summary>
public sealed class UpdateNoteCommandHandler(INoteStore noteStore, INoteIndexJobStore? indexJobStore = null)
{
    /// <summary>
    /// Atualiza a nota mediante a versão previamente obtida.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo da nota.</param>
    /// <param name="content">Novo conteúdo Markdown.</param>
    /// <param name="expectedETag">Versão esperada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado da alteração condicional.</returns>
    public Task<NoteWriteResult> HandleAsync(
        string vaultId,
        string path,
        string content,
        string expectedETag,
        CancellationToken cancellationToken) =>
        UpdateAndEnqueueAsync(vaultId, NotePath.Create(path), content, expectedETag, cancellationToken);

    private async Task<NoteWriteResult> UpdateAndEnqueueAsync(
        string vaultId,
        NotePath path,
        string content,
        string expectedETag,
        CancellationToken cancellationToken)
    {
        var result = await noteStore.UpdateAsync(vaultId, path, content, expectedETag, cancellationToken);
        if (result == NoteWriteResult.Updated && indexJobStore is not null)
        {
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content)));
            await indexJobStore.EnqueueAsync(vaultId, path.Value, $"note:{vaultId}:{path.Value}:{hash}", cancellationToken);
        }

        return result;
    }
}
