namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Coordena a leitura paginada de candidatos a vault.
/// </summary>
public sealed class ListVaultCandidatesQueryHandler(IVaultReader vaultReader)
{
    /// <summary>
    /// Executa a consulta paginada.
    /// </summary>
    /// <param name="query">Parâmetros da consulta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Página ordenada de candidatos.</returns>
    /// <exception cref="ArgumentOutOfRangeException">O limite não está entre 1 e 100.</exception>
    public Task<VaultCandidatePage> HandleAsync(
        ListVaultCandidatesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Limit, 100);

        return vaultReader.ReadCandidatesAsync(query.Cursor, query.Limit, cancellationToken);
    }
}