namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Entrega alertas administrativos de resíduos com lease e retry duráveis.
/// </summary>
public sealed class VaultResidueAlertProcessor(
    IVaultResidueAlertStore alertStore,
    IAdministrativeNotifier notifier,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan s_leaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan s_retryDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Reivindica e entrega um alerta administrativo pendente.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="true"/> quando um alerta foi processado.</returns>
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var workItem = await alertStore.ClaimNextAlertAsync(now, s_leaseDuration, cancellationToken);
        if (workItem is null)
        {
            return false;
        }

        try
        {
            await notifier.NotifyAsync(workItem.Alert, cancellationToken);
            _ = await alertStore.CompleteAlertAsync(workItem, null, null, cancellationToken);
        }
        catch (AdministrativeNotificationDeliveryException)
        {
            _ = await alertStore.CompleteAlertAsync(
                workItem,
                now.Add(s_retryDelay),
                "administrative-notification-delivery-failed",
                cancellationToken);
        }

        return true;
    }
}
