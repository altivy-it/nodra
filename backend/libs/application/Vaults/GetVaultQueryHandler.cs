using Nodra.Backend.Domain.Vaults;

namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Coordena a leitura de um vault autorizado pela camada de entrada.
/// </summary>
public sealed class GetVaultQueryHandler(IVaultReader vaultReader)
{
    /// <summary>
    /// Executa a consulta do vault.
    /// </summary>
    /// <param name="id">Identificador estável do vault.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O vault quando localizado; caso contrário, <see langword="null"/>.</returns>
    public Task<Vault?> HandleAsync(string id, CancellationToken cancellationToken) =>
        vaultReader.FindAsync(id, cancellationToken);
}
