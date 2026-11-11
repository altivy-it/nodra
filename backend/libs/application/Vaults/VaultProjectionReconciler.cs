using Nodra.Backend.Domain.Vaults;

namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Detecta projeções prontas cujo diretório canônico foi removido externamente.
/// </summary>
public sealed class VaultProjectionReconciler(
    IVaultProvisioningStore provisioningStore,
    IVaultFileSystemProvisioner fileSystemProvisioner,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Marca como degradadas as projeções prontas sem diretório físico confiável.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número de projeções marcadas como degradadas.</returns>
    public async Task<int> ReconcileAsync(CancellationToken cancellationToken)
    {
        var readyVaultIds = await provisioningStore.ListReadyVaultIdsAsync(cancellationToken);
        var degradedCount = 0;

        foreach (var vaultId in readyVaultIds)
        {
            if (await fileSystemProvisioner.ExistsAsync(vaultId, cancellationToken))
            {
                continue;
            }

            await provisioningStore.RegisterMissingDirectoryAsync(
                vaultId,
                timeProvider.GetUtcNow(),
                cancellationToken);
            degradedCount++;
        }

        return degradedCount;
    }
}
