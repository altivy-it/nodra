namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Provisiona o diretório canônico de um vault em armazenamento local confiável.
/// </summary>
public interface IVaultFileSystemProvisioner
{
    /// <summary>
    /// Garante que o diretório físico do vault exista de forma idempotente.
    /// </summary>
    /// <param name="vaultId">Identificador opaco gerado pelo servidor.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Uma tarefa que representa o provisionamento do diretório.</returns>
    /// <exception cref="ArgumentException">Quando o identificador do vault é inválido.</exception>
    /// <exception cref="UnauthorizedAccessException">Quando o caminho não é confiável.</exception>
    Task ProvisionAsync(string vaultId, CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se o diretório canônico de um vault existe sem seguir links simbólicos.
    /// </summary>
    /// <param name="vaultId">Identificador opaco gerado pelo servidor.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns><see langword="true"/> quando o diretório confiável existe.</returns>
    /// <exception cref="ArgumentException">Quando o identificador do vault é inválido.</exception>
    /// <exception cref="UnauthorizedAccessException">Quando o caminho não é confiável.</exception>
    Task<bool> ExistsAsync(string vaultId, CancellationToken cancellationToken);

    /// <summary>Lista os IDs válidos de diretórios diretamente sob a raiz confiável.</summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>IDs opacos de diretórios de vault encontrados.</returns>
    /// <exception cref="UnauthorizedAccessException">Quando a raiz não é confiável.</exception>
    Task<IReadOnlyList<string>> ListVaultIdsAsync(CancellationToken cancellationToken);

    /// <summary>Move um diretório de vault desconhecido para a quarentena confinada.</summary>
    /// <param name="vaultId">Identificador opaco do diretório órfão.</param>
    /// <param name="detectedAt">Instante UTC usado para identificar a quarentena.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns><see langword="true"/> quando o diretório foi movido.</returns>
    /// <exception cref="ArgumentException">Quando o identificador do vault é inválido.</exception>
    /// <exception cref="UnauthorizedAccessException">Quando o caminho não é confiável.</exception>
    Task<bool> QuarantineAsync(string vaultId, DateTimeOffset detectedAt, CancellationToken cancellationToken);
}
