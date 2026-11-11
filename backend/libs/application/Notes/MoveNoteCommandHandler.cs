using Nodra.Backend.Domain.Notes;

namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Coordena o movimento ou renomeação condicional de uma nota e a reconciliação da projeção.
/// </summary>
public sealed class MoveNoteCommandHandler(INoteStore noteStore, INoteIndexJobStore? indexJobStore = null)
{
    /// <summary>
    /// Move a nota mediante a versão previamente obtida, sem sobrescrever o destino.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="sourcePath">Caminho atual da nota.</param>
    /// <param name="destinationPath">Novo caminho da nota.</param>
    /// <param name="expectedETag">Versão esperada da origem.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado do movimento condicional.</returns>
    /// <exception cref="ArgumentException">Quando algum caminho é inválido ou os caminhos são iguais.</exception>
    public Task<NoteMoveResult> HandleAsync(
        string vaultId,
        string sourcePath,
        string destinationPath,
        string expectedETag,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vaultId) || vaultId.Length > 64)
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(vaultId));
        }

        var source = NotePath.Create(sourcePath);
        var destination = NotePath.Create(destinationPath);
        if (string.Equals(source.Value, destination.Value, StringComparison.Ordinal))
        {
            throw new ArgumentException("O destino deve ser diferente da origem.", nameof(destinationPath));
        }

        return MoveAndEnqueueAsync(vaultId, source, destination, expectedETag, cancellationToken);
    }

    private async Task<NoteMoveResult> MoveAndEnqueueAsync(
        string vaultId,
        NotePath source,
        NotePath destination,
        string expectedETag,
        CancellationToken cancellationToken)
    {
        var result = await noteStore.MoveAsync(vaultId, source, destination, expectedETag, cancellationToken);
        if (result == NoteMoveResult.Moved && indexJobStore is not null)
        {
            // A per-operation id keeps a later move back to the same paths from being deduplicated away.
            var operation = $"move:{vaultId}:{source.Value}:{destination.Value}:{expectedETag}:{Guid.NewGuid():N}";
            await indexJobStore.EnqueueAsync(vaultId, source.Value, $"{operation}:source", cancellationToken);
            await indexJobStore.EnqueueAsync(vaultId, destination.Value, $"{operation}:destination", cancellationToken);
        }

        return result;
    }
}
