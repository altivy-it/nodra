using Nodra.Backend.Domain.Vaults;

namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Executa uma operação durável de provisioning de vault por vez.
/// </summary>
public sealed class VaultProvisioningProcessor(
    IVaultProvisioningStore provisioningStore,
    IVaultFileSystemProvisioner fileSystemProvisioner,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Reivindica e processa uma operação pendente ou degradada.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="true"/> quando uma operação foi processada.</returns>
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var workItem = await provisioningStore.ClaimNextAsync(now, LeaseDuration, cancellationToken);
        if (workItem is null)
        {
            return false;
        }

        try
        {
            await fileSystemProvisioner.ProvisionAsync(workItem.Operation.VaultId, cancellationToken);
            workItem.Operation.TransitionTo(VaultLifecycleState.Ready);
            await provisioningStore.CompleteAsync(workItem, null, null, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            workItem.Operation.TransitionTo(VaultLifecycleState.Failed);
            await provisioningStore.CompleteAsync(workItem, null, "filesystem-access-denied", cancellationToken);
        }
        catch (IOException)
        {
            workItem.Operation.TransitionTo(VaultLifecycleState.Degraded);
            await provisioningStore.CompleteAsync(
                workItem,
                now.Add(RetryDelay),
                "filesystem-provisioning-failed",
                cancellationToken);
        }

        return true;
    }
}
