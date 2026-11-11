using Nodra.Backend.Application.Notes;
using Nodra.Backend.Domain.Notes;
using Microsoft.EntityFrameworkCore;

namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Persiste a projeção derivada sem substituir o filesystem como fonte canônica.
/// </summary>
public sealed class PostgresNoteProjectionStore(PostgresDbContext dbContext) : INoteProjectionStore
{
    /// <inheritdoc />
    public async Task UpsertAsync(string vaultId, NoteDocument note, CancellationToken cancellationToken)
    {
        var projection = NoteProjectionBuilder.Build(vaultId, note, DateTimeOffset.UtcNow);
        var current = await dbContext.NoteProjections.SingleOrDefaultAsync(
            record => record.VaultId == projection.VaultId && record.Path == projection.Path,
            cancellationToken);

        if (current is null)
        {
            dbContext.NoteProjections.Add(projection);
        }
        else
        {
            current.ContentHash = projection.ContentHash;
            current.Title = projection.Title;
            current.PropertiesJson = projection.PropertiesJson;
            current.TagsJson = projection.TagsJson;
            current.HeadingsJson = projection.HeadingsJson;
            current.WikiLinksJson = projection.WikiLinksJson;
            current.MarkdownLinksJson = projection.MarkdownLinksJson;
            current.IndexedAt = projection.IndexedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string vaultId, NotePath path, CancellationToken cancellationToken)
    {
        await dbContext.NoteProjections
            .Where(record => record.VaultId == vaultId && record.Path == path.Value)
            .ExecuteDeleteAsync(cancellationToken);
    }
}