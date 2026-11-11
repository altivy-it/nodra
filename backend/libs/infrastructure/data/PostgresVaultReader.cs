using Nodra.Backend.Application.Vaults;
using Nodra.Backend.Domain.Vaults;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Lê vaults da projeção PostgreSQL sem expor modelos de persistência.
/// </summary>
public sealed class PostgresVaultReader(PostgresDbContext dbContext) : IVaultReader
{
    private const string CursorPrefix = "v1:";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <inheritdoc />
    public async Task<Vault?> FindAsync(string id, CancellationToken cancellationToken)
    {
        var record = await dbContext.Vaults
            .AsNoTracking()
            .Where(vault => vault.Id == id)
            .Select(vault => new { vault.Id, vault.Name })
            .SingleOrDefaultAsync(cancellationToken);

        return record is null ? null : Vault.Create(record.Id, record.Name);
    }

    /// <inheritdoc />
    public async Task<VaultCandidatePage> ReadCandidatesAsync(
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        var lastId = DecodeCursor(cursor);
        var query = dbContext.Vaults.AsNoTracking();
        if (lastId is not null)
        {
            query = query.Where(vault => vault.Id.CompareTo(lastId) > 0);
        }

        var records = await query
            .OrderBy(vault => vault.Id)
            .Select(vault => new VaultCandidate(vault.Id, vault.Name))
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = records.Count > limit;
        if (hasMore)
        {
            records.RemoveAt(records.Count - 1);
        }

        var nextCursor = hasMore ? EncodeCursor(records[^1].Id) : null;
        return new VaultCandidatePage(records, nextCursor);
    }

    private static string EncodeCursor(string id)
    {
        var bytes = StrictUtf8.GetBytes(CursorPrefix + id);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string? DecodeCursor(string? cursor)
    {
        if (cursor is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(cursor) || cursor.Length > 512)
        {
            throw new InvalidVaultCursorException();
        }

        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
            var value = StrictUtf8.GetString(Convert.FromBase64String(base64));
            if (!value.StartsWith(CursorPrefix, StringComparison.Ordinal))
            {
                throw new InvalidVaultCursorException();
            }

            var id = value[CursorPrefix.Length..];
            _ = Vault.Create(id, id);
            return id;
        }
        catch (InvalidVaultCursorException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is FormatException or DecoderFallbackException or ArgumentException)
        {
            throw new InvalidVaultCursorException(exception);
        }
    }
}
