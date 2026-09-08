using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Define a porta de persistência canônica de notas Markdown.
/// </summary>
public interface INoteStore
{
    /// <summary>
    /// Lista as notas Markdown do vault.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Notas ordenadas pelo caminho relativo.</returns>
    Task<IReadOnlyList<NoteDocument>> ListAsync(string vaultId, CancellationToken cancellationToken);

    /// <summary>
    /// Localiza uma nota pelo caminho relativo.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo e validado da nota.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A nota quando existir; caso contrário, <see langword="null"/>.</returns>
    Task<NoteDocument?> FindAsync(string vaultId, NotePath path, CancellationToken cancellationToken);

    /// <summary>
    /// Cria uma nota caso o caminho ainda não exista no vault.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo e validado da nota.</param>
    /// <param name="content">Conteúdo Markdown canônico.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns><see langword="true"/> quando a nota for criada; caso contrário, <see langword="false"/>.</returns>
    Task<bool> TryCreateAsync(string vaultId, NotePath path, string content, CancellationToken cancellationToken);

    /// <summary>
    /// Atualiza a nota somente quando a versão esperada ainda é atual.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo e validado da nota.</param>
    /// <param name="content">Novo conteúdo Markdown canônico.</param>
    /// <param name="expectedETag">Versão recebida anteriormente.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado da alteração condicional.</returns>
    Task<NoteWriteResult> UpdateAsync(
        string vaultId,
        NotePath path,
        string content,
        string expectedETag,
        CancellationToken cancellationToken);

    /// <summary>
    /// Exclui a nota somente quando a versão esperada ainda é atual.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="path">Caminho relativo e validado da nota.</param>
    /// <param name="expectedETag">Versão recebida anteriormente.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado da alteração condicional.</returns>
    Task<NoteWriteResult> DeleteAsync(
        string vaultId,
        NotePath path,
        string expectedETag,
        CancellationToken cancellationToken);
}