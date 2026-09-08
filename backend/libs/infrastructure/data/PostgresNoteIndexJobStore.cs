using Altivy.Backend.Application.Notes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Implementa a fila de indexação com atualização condicional por lease.
/// </summary>
public sealed class PostgresNoteIndexJobStore(PostgresDbContext dbContext) : INoteIndexJobStore
{
    private const int Pending = 0;
    private const int Processing = 1;
    private const int Completed = 2;

    public async Task EnqueueAsync(string vaultId, string? path, string idempotencyKey, CancellationToken cancellationToken)
    {
        var exists = await dbContext.NoteIndexJobs.AnyAsync(
            job => job.IdempotencyKey == idempotencyKey,
            cancellationToken);
        if (exists)
        {
            return;
        }

        var record = new NoteIndexJobRecord
        {
            JobId = Guid.NewGuid(),
            VaultId = vaultId,
            Path = path,
            IdempotencyKey = idempotencyKey,
            State = Pending,
            AttemptCount = 0,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        dbContext.NoteIndexJobs.Add(record);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            dbContext.Entry(record).State = EntityState.Detached;
        }
    }

    public async Task<NoteIndexJob?> ClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var leaseId = Guid.NewGuid();
        var candidate = await dbContext.NoteIndexJobs
            .AsNoTracking()
            .Where(job =>
                job.State == Pending && job.NextAttemptAt <= now ||
                job.State == Processing && job.LeaseExpiresAt <= now)
            .OrderBy(job => job.NextAttemptAt)
            .ThenBy(job => job.JobId)
            .Select(job => job.JobId)
            .FirstOrDefaultAsync(cancellationToken);
        if (candidate == Guid.Empty)
        {
            return null;
        }

        var claimed = await dbContext.NoteIndexJobs
            .Where(job => job.JobId == candidate)
            .Where(job =>
                job.State == Pending && job.NextAttemptAt <= now ||
                job.State == Processing && job.LeaseExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.State, Processing)
                .SetProperty(job => job.LeaseId, leaseId)
                .SetProperty(job => job.LeaseExpiresAt, now.Add(leaseDuration))
                .SetProperty(job => job.AttemptCount, job => job.AttemptCount + 1)
                .SetProperty(job => job.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(job => job.LastError, (string?)null), cancellationToken);
        if (claimed == 0)
        {
            return null;
        }

        var record = await dbContext.NoteIndexJobs.AsNoTracking()
            .SingleAsync(job => job.JobId == candidate && job.LeaseId == leaseId, cancellationToken);
        return new NoteIndexJob(record.JobId, record.VaultId, record.Path, record.AttemptCount, leaseId);
    }

    public async Task<bool> CompleteAsync(
        NoteIndexJob job,
        DateTimeOffset? nextAttemptAt,
        string? lastError,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.NoteIndexJobs
            .Where(record => record.JobId == job.JobId && record.LeaseId == job.LeaseId && record.State == Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(record => record.State, nextAttemptAt is null ? Completed : Pending)
                .SetProperty(record => record.NextAttemptAt, nextAttemptAt)
                .SetProperty(record => record.LastError, lastError)
                .SetProperty(record => record.LeaseId, (Guid?)null)
                .SetProperty(record => record.LeaseExpiresAt, (DateTimeOffset?)null), cancellationToken);
        return updated > 0;
    }
}