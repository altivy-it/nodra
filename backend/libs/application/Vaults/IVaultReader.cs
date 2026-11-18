using Nodra.Backend.Domain.Vaults;

namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Expõe a leitura de vaults sem acoplar a aplicação ao armazenamento.
/// </summary>
public interface IVaultReader
{
    /// <summary>
    /// Localiza um vault pelo identificador estável.
    /// </summary>
    /// <param name="id">Identificador do vault.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O vault quando existe; caso contrário, <see langword="null"/>.</returns>
    Task<Vault?> FindAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Lê uma página ordenada de candidatos sem aplicar autorização.
    /// </summary>
    /// <param name="cursor">Cursor opaco da página anterior.</param>
    /// <param name="limit">Quantidade máxima de candidatos.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Página de candidatos e cursor para continuação.</returns>
    Task<VaultCandidatePage> ReadCandidatesAsync(
        string? cursor,
        int limit,
        CancellationToken cancellationToken);
}
