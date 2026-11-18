using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nodra.Backend.Application.Vaults;
using Nodra.Notes.Worker.Observability;

namespace Nodra.Notes.Worker.Vaults;

/// <summary>
/// Consome operações de provisioning de vault persistidas no PostgreSQL.
/// </summary>
public sealed class VaultProvisioningBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    WorkerTelemetry telemetry,
    ILogger<VaultProvisioningBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan s_pollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan s_reconciliationInterval = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextReconciliationAt = timeProvider.GetUtcNow();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await ProcessNextAsync(stoppingToken))
                {
                    telemetry.RecordProvisioningProcessed();
                }

                if (timeProvider.GetUtcNow() >= nextReconciliationAt)
                {
                    await ReconcileAsync(stoppingToken);
                    await ReconcileOrphanedDirectoriesAsync(stoppingToken);
                    await ScheduleRetentionWarningsAsync(stoppingToken);
                    nextReconciliationAt = timeProvider.GetUtcNow().Add(s_reconciliationInterval);
                }

                while (await ProcessNextAlertAsync(stoppingToken))
                {
                    telemetry.RecordAlertProcessed();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (DbException exception)
            {
                telemetry.RecordFailure("database");
                logger.LogError(exception, "Falha ao processar uma operação de provisioning de vault.");
            }
            catch (IOException exception)
            {
                telemetry.RecordFailure("filesystem");
                logger.LogError(exception, "Falha ao processar uma operação de provisioning de vault.");
            }
            catch (UnauthorizedAccessException exception)
            {
                telemetry.RecordFailure("filesystem-access");
                logger.LogError(exception, "Falha ao processar uma operação de provisioning de vault.");
            }
            catch (TimeoutException exception)
            {
                telemetry.RecordFailure("timeout");
                logger.LogError(exception, "Falha ao processar uma operação de provisioning de vault.");
            }
            catch (InvalidOperationException exception)
            {
                telemetry.RecordFailure("configuration");
                logger.LogError(exception, "Falha ao processar uma operação de provisioning de vault.");
            }

            await Task.Delay(s_pollingInterval, stoppingToken);
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<VaultProvisioningProcessor>();
        return await processor.ProcessNextAsync(cancellationToken);
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var reconciler = scope.ServiceProvider.GetRequiredService<VaultProjectionReconciler>();
        var degradedCount = await reconciler.ReconcileAsync(cancellationToken);
        if (degradedCount > 0)
        {
            telemetry.RecordResiduesDetected("missing-directory", degradedCount);
            logger.LogWarning("Reconciliação marcou {VaultCount} vaults como degradados.", degradedCount);
        }
    }

    private async Task<bool> ProcessNextAlertAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<VaultResidueAlertProcessor>();
        return await processor.ProcessNextAsync(cancellationToken);
    }

    private async Task ReconcileOrphanedDirectoriesAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var reconciler = scope.ServiceProvider.GetRequiredService<VaultOrphanDirectoryReconciler>();
        var quarantinedCount = await reconciler.ReconcileAsync(cancellationToken);
        if (quarantinedCount > 0)
        {
            telemetry.RecordResiduesDetected("orphaned-directory", quarantinedCount);
            logger.LogWarning("Reconciliação isolou {VaultCount} diretórios órfãos em quarentena.", quarantinedCount);
        }
    }

    private async Task ScheduleRetentionWarningsAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var alertStore = scope.ServiceProvider.GetRequiredService<IVaultResidueAlertStore>();
        var scheduledCount = await alertStore.ScheduleRetentionWarningsAsync(timeProvider.GetUtcNow(), cancellationToken);
        if (scheduledCount > 0)
        {
            logger.LogWarning("Retenção venceu para {OccurrenceCount} ocorrências de resíduo.", scheduledCount);
        }
    }
}
