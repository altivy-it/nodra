using Nodra.Backend.Domain.Notes;

namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Coordena o movimento ou renomeação de uma pasta e a reconciliação das notas contidas.
/// </summary>
public sealed class MoveFolderCommandHandler(IFolderStore folderStore, INoteIndexJobStore? indexJobStore = null)
{
    /// <summary>
    /// Move a pasta e enfileira a reconciliação de cada nota cujo caminho mudou.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="sourcePath">Caminho atual da pasta.</param>
    /// <param name="destinationPath">Novo caminho da pasta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado do movimento.</returns>
    /// <exception cref="ArgumentException">Quando algum caminho é inválido, igual à origem ou interno a ela.</exception>
    public Task<FolderMoveResult> HandleAsync(
        string vaultId,
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vaultId) || vaultId.Length > 64)
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(vaultId));
        }

        var source = FolderPath.Create(sourcePath);
        var destination = FolderPath.Create(destinationPath);
        if (string.Equals(source.Value, destination.Value, StringComparison.OrdinalIgnoreCase) ||
            destination.Value.StartsWith(source.Value + "/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("O destino não pode ser a própria pasta nem estar dentro dela.", nameof(destinationPath));
        }

        return MoveAndEnqueueAsync(vaultId, source, destination, cancellationToken);
    }

    private async Task<FolderMoveResult> MoveAndEnqueueAsync(
        string vaultId,
        FolderPath source,
        FolderPath destination,
        CancellationToken cancellationToken)
    {
        var outcome = await folderStore.MoveAsync(vaultId, source, destination, cancellationToken);
        if (outcome.Result == FolderMoveResult.Moved && indexJobStore is not null)
        {
            var operation = $"move-folder:{vaultId}:{source.Value}:{destination.Value}:{Guid.NewGuid():N}";
            foreach (var (from, to) in outcome.MovedNotes)
            {
                await indexJobStore.EnqueueAsync(vaultId, from, $"{operation}:{from}:source", cancellationToken);
                await indexJobStore.EnqueueAsync(vaultId, to, $"{operation}:{to}:destination", cancellationToken);
            }
        }

        return outcome.Result;
    }
}
