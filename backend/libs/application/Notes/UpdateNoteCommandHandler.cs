using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Coordena a alteração condicional de uma nota existente.
/// </summary>
public sealed class UpdateNoteCommandHandler(INoteStore noteStore)
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
        noteStore.UpdateAsync(vaultId, NotePath.Create(path), content, expectedETag, cancellationToken);
}
