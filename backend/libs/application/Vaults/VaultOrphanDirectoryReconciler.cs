namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Isola diretórios válidos que não pertencem a um vault ou provisioning conhecido.
/// </summary>
public sealed class VaultOrphanDirectoryReconciler(
    IVaultProvisioningStore provisioningStore,
    IVaultFileSystemProvisioner fileSystemProvisioner,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Move diretórios órfãos para quarentena e registra a ocorrência para alerta.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número de diretórios efetivamente movidos para quarentena.</returns>
    public async Task<int> ReconcileAsync(CancellationToken cancellationToken)
    {
        var knownVaultIds = await provisioningStore.ListKnownVaultIdsAsync(cancellationToken);
        var vaultIds = await fileSystemProvisioner.ListVaultIdsAsync(cancellationToken);
        var quarantinedCount = 0;

        foreach (var vaultId in vaultIds)
        {
            if (knownVaultIds.Contains(vaultId))
            {
                continue;
            }

            var detectedAt = timeProvider.GetUtcNow();
            await provisioningStore.RegisterOrphanedDirectoryAsync(vaultId, detectedAt, cancellationToken);
            if (!await fileSystemProvisioner.QuarantineAsync(vaultId, detectedAt, cancellationToken))
            {
                continue;
            }

            quarantinedCount++;
        }

        return quarantinedCount;
    }
}
