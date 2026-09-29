namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Lista somente operações da lixeira pertencentes ao sujeito autenticado.
/// </summary>
public sealed class ListTrashQueryHandler(ITrashStore trashStore)
{
    /// <summary>
    /// Retorna operações ordenadas pela exclusão mais recente.
    /// </summary>
    public async Task<TrashPage> HandleAsync(
        string vaultId,
        string subject,
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vaultId) || vaultId.Length > 64)
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(vaultId));
        }
        if (limit is < 1 or > 100)
        {
            throw new ArgumentException("O limite da página é inválido.", nameof(limit));
        }

        var entries = await trashStore.ListTrashAsync(vaultId, subject, cancellationToken);
        var startIndex = 0;
        if (cursor is not null)
        {
            if (!Guid.TryParseExact(cursor, "N", out var cursorId))
            {
                throw new ArgumentException("O cursor da lixeira é inválido.", nameof(cursor));
            }
            var cursorIndex = entries.ToList().FindIndex(entry => entry.Id == cursorId);
            if (cursorIndex < 0)
            {
                throw new ArgumentException("O cursor da lixeira expirou.", nameof(cursor));
            }
            startIndex = cursorIndex + 1;
        }

        var page = entries.Skip(startIndex).Take(limit).ToArray();
        var nextCursor = startIndex + page.Length < entries.Count && page.Length > 0
            ? page[^1].Id.ToString("N")
            : null;
        return new TrashPage(page, nextCursor);
    }
}

/// <summary>
/// Página limitada de operações da lixeira.
/// </summary>
public sealed record TrashPage(IReadOnlyList<TrashEntry> Items, string? NextCursor);