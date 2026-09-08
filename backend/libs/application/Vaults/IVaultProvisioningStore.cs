using Altivy.Backend.Domain.Vaults;

namespace Altivy.Backend.Application.Vaults;

/// <summary>
/// Porta para persistir operações de provisioning sem conhecer o banco.
/// </summary>
public interface IVaultProvisioningStore
{
    /// <summary>Localiza uma operação pela chave de idempotência e sujeito.</summary>
    /// <param name="subject">Sujeito proprietário da operação.</param>
    /// <param name="idempotencyKey">Chave de retry fornecida pelo cliente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A operação existente ou <see langword="null"/>.</returns>
    Task<VaultProvisioningOperation?> FindByIdempotencyKeyAsync(
        string subject,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>Persiste uma operação nova.</summary>
    /// <param name="operation">Operação a persistir.</param>
    /// <param name="idempotencyKey">Chave usada para evitar duplicidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(VaultProvisioningOperation operation, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>Reivindica uma operação elegível para processamento.</summary>
    /// <param name="now">Instante atual em UTC.</param>
    /// <param name="leaseDuration">Duração máxima da reivindicação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A operação reivindicada ou <see langword="null"/> quando não houver trabalho.</returns>
    Task<VaultProvisioningWorkItem?> ClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>Persiste o resultado de uma operação previamente reivindicada.</summary>
    /// <param name="workItem">Operação e lease que identificam o processamento.</param>
    /// <param name="nextAttemptAt">Instante do próximo retry, quando aplicável.</param>
    /// <param name="lastError">Código sanitizado do último erro, quando aplicável.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="true"/> quando a operação ainda era proprietária do lease.</returns>
    Task<bool> CompleteAsync(
        VaultProvisioningWorkItem workItem,
        DateTimeOffset? nextAttemptAt,
        string? lastError,
        CancellationToken cancellationToken);

    /// <summary>Lista os vaults cuja projeção está pronta para uso.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificadores opacos dos vaults prontos.</returns>
    Task<IReadOnlyList<string>> ListReadyVaultIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Registra a ausência do diretório confiável e degrada a projeção do vault.
    /// </summary>
    /// <param name="vaultId">Identificador opaco do vault.</param>
    /// <param name="detectedAt">Instante UTC da detecção.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Uma tarefa que representa o registro idempotente da ocorrência.</returns>
    Task RegisterMissingDirectoryAsync(
        string vaultId,
        DateTimeOffset detectedAt,
        CancellationToken cancellationToken);

    /// <summary>Lista IDs que possuem projeção ou operação de provisioning conhecida.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificadores opacos conhecidos pelo controle operacional.</returns>
    Task<IReadOnlySet<string>> ListKnownVaultIdsAsync(CancellationToken cancellationToken);

    /// <summary>Registra um diretório órfão já isolado em quarentena.</summary>
    /// <param name="vaultId">Identificador opaco do diretório órfão.</param>
    /// <param name="detectedAt">Instante UTC da detecção.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task RegisterOrphanedDirectoryAsync(
        string vaultId,
        DateTimeOffset detectedAt,
        CancellationToken cancellationToken);
}
