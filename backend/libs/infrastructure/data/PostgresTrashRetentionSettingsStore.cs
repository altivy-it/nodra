using Microsoft.EntityFrameworkCore;
using Nodra.Backend.Application.Notes;

namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Implementa o parâmetro de retenção por sujeito no PostgreSQL.
/// </summary>
public sealed class PostgresTrashRetentionSettingsStore(PostgresDbContext db) : ITrashRetentionSettingsStore
{
    public async Task<int?> GetRetentionDaysAsync(string subject, CancellationToken cancellationToken) =>
        await db.UserTrashRetentionSettings
            .AsNoTracking()
            .Where(record => record.Subject == subject)
            .Select(record => record.RetentionDays)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task SetRetentionDaysAsync(string subject, int? retentionDays, CancellationToken cancellationToken)
    {
        var current = await db.UserTrashRetentionSettings
            .SingleOrDefaultAsync(record => record.Subject == subject, cancellationToken);
        if (current is null)
        {
            _ = db.UserTrashRetentionSettings.Add(new UserTrashRetentionSettingRecord
            {
                Subject = subject,
                RetentionDays = retentionDays,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        else
        {
            current.RetentionDays = retentionDays;
            current.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        _ = await db.SaveChangesAsync(cancellationToken);
    }
}
