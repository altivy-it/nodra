using Microsoft.EntityFrameworkCore;
using Nodra.Backend.Application.Vaults;
using Nodra.Backend.Domain.Vaults;

namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Persiste operações de provisioning no PostgreSQL sem copiar autorização local.
/// </summary>
public sealed class PostgresVaultProvisioningStore(PostgresDbContext dbContext) : IVaultProvisioningStore, IVaultResidueAlertStore
{
    /// <inheritdoc />
    public async Task<VaultProvisioningOperation?> FindByIdempotencyKeyAsync(
        string subject,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.VaultProvisioningOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                operation => operation.Subject == subject && operation.IdempotencyKey == idempotencyKey,
                cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        VaultProvisioningOperation operation,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = dbContext.VaultProvisioningOperations.Add(new VaultProvisioningOperationRecord
        {
            OperationId = operation.OperationId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            VaultId = operation.VaultId,
            Name = operation.Name,
            Subject = operation.Subject,
            IdempotencyKey = idempotencyKey,
            State = (int)operation.State,
            AttemptCount = 0
        });
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VaultProvisioningWorkItem?> ClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var leaseId = Guid.NewGuid();
        var leaseExpiresAt = now.Add(leaseDuration);
        var candidate = await dbContext.VaultProvisioningOperations
            .AsNoTracking()
            .Where(operation =>
                operation.State == (int)VaultLifecycleState.Pending ||
                (operation.State == (int)VaultLifecycleState.Degraded && operation.NextAttemptAt <= now) ||
                (operation.State == (int)VaultLifecycleState.Provisioning && operation.LeaseExpiresAt <= now))
            .OrderBy(operation => operation.NextAttemptAt)
            .ThenBy(operation => operation.OperationId)
            .Select(operation => operation.OperationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (candidate == Guid.Empty)
        {
            return null;
        }

        var claimed = await dbContext.VaultProvisioningOperations
            .Where(operation => operation.OperationId == candidate)
            .Where(operation =>
                operation.State == (int)VaultLifecycleState.Pending ||
                (operation.State == (int)VaultLifecycleState.Degraded && operation.NextAttemptAt <= now) ||
                (operation.State == (int)VaultLifecycleState.Provisioning && operation.LeaseExpiresAt <= now))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(operation => operation.State, (int)VaultLifecycleState.Provisioning)
                    .SetProperty(operation => operation.LeaseId, leaseId)
                    .SetProperty(operation => operation.LeaseExpiresAt, leaseExpiresAt)
                    .SetProperty(operation => operation.AttemptCount, operation => operation.AttemptCount + 1)
                    .SetProperty(operation => operation.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(operation => operation.LastError, (string?)null),
                cancellationToken);
        if (claimed == 0)
        {
            return null;
        }

        var record = await dbContext.VaultProvisioningOperations
            .AsNoTracking()
            .SingleAsync(operation => operation.OperationId == candidate && operation.LeaseId == leaseId, cancellationToken);
        return new VaultProvisioningWorkItem(ToDomain(record), leaseId, record.AttemptCount);
    }

    /// <inheritdoc />
    public async Task<bool> CompleteAsync(
        VaultProvisioningWorkItem workItem,
        DateTimeOffset? nextAttemptAt,
        string? lastError,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var updated = await dbContext.VaultProvisioningOperations
            .Where(operation =>
                operation.OperationId == workItem.Operation.OperationId &&
                operation.LeaseId == workItem.LeaseId &&
                operation.State == (int)VaultLifecycleState.Provisioning)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(operation => operation.State, (int)workItem.Operation.State)
                    .SetProperty(operation => operation.NextAttemptAt, nextAttemptAt)
                    .SetProperty(operation => operation.LastError, lastError)
                    .SetProperty(operation => operation.LeaseId, (Guid?)null)
                    .SetProperty(operation => operation.LeaseExpiresAt, (DateTimeOffset?)null),
                cancellationToken);
        if (updated == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var vault = await dbContext.Vaults.SingleOrDefaultAsync(
            record => record.Id == workItem.Operation.VaultId,
            cancellationToken);
        if (vault is null && workItem.Operation.State == VaultLifecycleState.Ready)
        {
            _ = dbContext.Vaults.Add(new VaultRecord
            {
                Id = workItem.Operation.VaultId,
                Name = workItem.Operation.Name,
                State = VaultLifecycleState.Ready
            });
        }
        else
        {
            vault?.State = workItem.Operation.State;
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListReadyVaultIdsAsync(CancellationToken cancellationToken) =>
        await dbContext.Vaults
            .AsNoTracking()
            .Where(record => record.State == VaultLifecycleState.Ready)
            .OrderBy(record => record.Id)
            .Select(record => record.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task RegisterMissingDirectoryAsync(
        string vaultId,
        DateTimeOffset detectedAt,
        CancellationToken cancellationToken)
    {
        _ = Vault.Create(vaultId, vaultId);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        _ = await dbContext.Vaults
            .Where(record => record.Id == vaultId && record.State == VaultLifecycleState.Ready)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(record => record.State, VaultLifecycleState.Degraded),
                cancellationToken);

        var occurrence = await dbContext.VaultResidueOccurrences.SingleOrDefaultAsync(
            record => record.VaultId == vaultId && record.Type == "missing-directory/v1",
            cancellationToken);
        if (occurrence is null)
        {
            _ = dbContext.VaultResidueOccurrences.Add(new VaultResidueOccurrenceRecord
            {
                OccurrenceId = Guid.NewGuid(),
                VaultId = vaultId,
                Type = "missing-directory/v1",
                FirstDetectedAt = detectedAt,
                LastDetectedAt = detectedAt,
                AlertPending = true,
                AlertAttemptCount = 0,
                NextAlertAttemptAt = detectedAt,
                AlertActionRequired = "investigate-vault-residue"
            });
        }
        else
        {
            occurrence.LastDetectedAt = detectedAt;
            occurrence.AlertPending = true;
            occurrence.NextAlertAttemptAt = detectedAt;
            occurrence.AlertActionRequired = "investigate-vault-residue";
            occurrence.RetentionWarningScheduledAt = null;
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> ListKnownVaultIdsAsync(CancellationToken cancellationToken)
    {
        var vaultIds = await dbContext.Vaults
            .AsNoTracking()
            .Select(record => record.Id)
            .Concat(dbContext.VaultProvisioningOperations.AsNoTracking().Select(record => record.VaultId))
            .ToListAsync(cancellationToken);
        return vaultIds.ToHashSet(StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public async Task RegisterOrphanedDirectoryAsync(
        string vaultId,
        DateTimeOffset detectedAt,
        CancellationToken cancellationToken)
    {
        _ = Vault.Create(vaultId, vaultId);
        var occurrence = await dbContext.VaultResidueOccurrences.SingleOrDefaultAsync(
            record => record.VaultId == vaultId && record.Type == "orphaned-directory/v1",
            cancellationToken);
        if (occurrence is null)
        {
            _ = dbContext.VaultResidueOccurrences.Add(new VaultResidueOccurrenceRecord
            {
                OccurrenceId = Guid.NewGuid(),
                VaultId = vaultId,
                Type = "orphaned-directory/v1",
                FirstDetectedAt = detectedAt,
                LastDetectedAt = detectedAt,
                AlertPending = true,
                AlertAttemptCount = 0,
                NextAlertAttemptAt = detectedAt,
                AlertActionRequired = "investigate-vault-residue"
            });
        }
        else
        {
            occurrence.LastDetectedAt = detectedAt;
            occurrence.AlertPending = true;
            occurrence.NextAlertAttemptAt = detectedAt;
            occurrence.AlertActionRequired = "investigate-vault-residue";
            occurrence.RetentionWarningScheduledAt = null;
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VaultResidueAlertWorkItem?> ClaimNextAlertAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var leaseId = Guid.NewGuid();
        var leaseExpiresAt = now.Add(leaseDuration);
        var candidate = await dbContext.VaultResidueOccurrences
            .AsNoTracking()
            .Where(record =>
                record.AlertPending &&
                record.NextAlertAttemptAt <= now &&
                (record.AlertLeaseExpiresAt == null || record.AlertLeaseExpiresAt <= now))
            .OrderBy(record => record.NextAlertAttemptAt)
            .ThenBy(record => record.OccurrenceId)
            .Select(record => record.OccurrenceId)
            .FirstOrDefaultAsync(cancellationToken);
        if (candidate == Guid.Empty)
        {
            return null;
        }

        var claimed = await dbContext.VaultResidueOccurrences
            .Where(record => record.OccurrenceId == candidate)
            .Where(record =>
                record.AlertPending &&
                record.NextAlertAttemptAt <= now &&
                (record.AlertLeaseExpiresAt == null || record.AlertLeaseExpiresAt <= now))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(record => record.AlertLeaseId, leaseId)
                    .SetProperty(record => record.AlertLeaseExpiresAt, leaseExpiresAt)
                    .SetProperty(record => record.AlertAttemptCount, record => record.AlertAttemptCount + 1)
                    .SetProperty(record => record.AlertLastError, (string?)null),
                cancellationToken);
        if (claimed == 0)
        {
            return null;
        }

        var record = await dbContext.VaultResidueOccurrences
            .AsNoTracking()
            .SingleAsync(occurrence => occurrence.OccurrenceId == candidate && occurrence.AlertLeaseId == leaseId, cancellationToken);
        return new VaultResidueAlertWorkItem(
            new VaultResidueAlert(
                record.OccurrenceId,
                record.VaultId,
                record.Type,
                record.FirstDetectedAt,
                record.LastDetectedAt,
                record.AlertAttemptCount,
                record.AlertActionRequired),
            leaseId);
    }

    /// <inheritdoc />
    public async Task<bool> CompleteAlertAsync(
        VaultResidueAlertWorkItem workItem,
        DateTimeOffset? nextAttemptAt,
        string? lastError,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.VaultResidueOccurrences
            .Where(record =>
                record.OccurrenceId == workItem.Alert.OccurrenceId &&
                record.AlertLeaseId == workItem.LeaseId &&
                record.AlertPending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(record => record.AlertPending, nextAttemptAt is not null)
                    .SetProperty(record => record.NextAlertAttemptAt, nextAttemptAt)
                    .SetProperty(record => record.AlertLastError, lastError)
                    .SetProperty(record => record.AlertLeaseId, (Guid?)null)
                    .SetProperty(record => record.AlertLeaseExpiresAt, (DateTimeOffset?)null),
                cancellationToken);
        return updated == 1;
    }

    /// <inheritdoc />
    public async Task<int> ScheduleRetentionWarningsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var retentionDeadline = now.AddDays(-30);
        return await dbContext.VaultResidueOccurrences
            .Where(record =>
                !record.AlertPending &&
                record.RetentionWarningScheduledAt == null &&
                record.LastDetectedAt <= retentionDeadline)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(record => record.AlertPending, true)
                    .SetProperty(record => record.NextAlertAttemptAt, now)
                    .SetProperty(record => record.AlertActionRequired, "retention-expired")
                    .SetProperty(record => record.RetentionWarningScheduledAt, now),
                cancellationToken);
    }

    private static VaultProvisioningOperation ToDomain(VaultProvisioningOperationRecord record)
    {
        var operation = VaultProvisioningOperation.Create(
            record.OperationId,
            record.VaultId,
            record.Name,
            record.Subject);
        switch ((VaultLifecycleState)record.State)
        {
            case VaultLifecycleState.Pending:
                return operation;
            case VaultLifecycleState.Provisioning:
                operation.TransitionTo(VaultLifecycleState.Provisioning);
                break;
            case VaultLifecycleState.Ready:
                operation.TransitionTo(VaultLifecycleState.Provisioning);
                operation.TransitionTo(VaultLifecycleState.Ready);
                break;
            case VaultLifecycleState.Degraded:
                operation.TransitionTo(VaultLifecycleState.Provisioning);
                operation.TransitionTo(VaultLifecycleState.Degraded);
                break;
            case VaultLifecycleState.Archived:
                operation.TransitionTo(VaultLifecycleState.Provisioning);
                operation.TransitionTo(VaultLifecycleState.Ready);
                operation.TransitionTo(VaultLifecycleState.Archived);
                break;
            case VaultLifecycleState.Failed:
                operation.TransitionTo(VaultLifecycleState.Failed);
                break;
            default:
                throw new InvalidOperationException("O estado persistido do provisioning é inválido.");
        }

        return operation;
    }
}
