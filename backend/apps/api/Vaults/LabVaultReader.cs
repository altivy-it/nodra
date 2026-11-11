using Nodra.Backend.Application.Vaults;
using Nodra.Backend.Domain.Vaults;
using System.Text;

namespace Nodra.Notes.Api.Vaults;

/// <summary>
/// Reader temporário para o vault sintético do laboratório.
/// </summary>
public sealed class LabVaultReader : IVaultReader
{
    private const string CursorPrefix = "v1:";
    private static readonly VaultCandidate[] Candidates =
    [
        new("lab-0001", "Laboratório principal"),
        new("lab-0002", "Laboratório secundário")
    ];

    /// <summary>
    /// Localiza somente o vault sintético configurado para os testes locais.
    /// </summary>
    /// <param name="id">Identificador estável do vault.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O vault de laboratório quando o ID é conhecido.</returns>
    public Task<Vault?> FindAsync(string id, CancellationToken cancellationToken)
    {
        var candidate = Candidates.SingleOrDefault(candidate => candidate.Id == id);
        var vault = candidate is null ? null : Vault.Create(candidate.Id, candidate.Name);
        return Task.FromResult(vault);
    }

    /// <inheritdoc />
    public Task<VaultCandidatePage> ReadCandidatesAsync(
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lastId = DecodeCursor(cursor);
        var candidates = lastId is null
            ? Candidates
            : Candidates.Where(candidate => string.CompareOrdinal(candidate.Id, lastId) > 0).ToArray();
        var items = candidates.Take(limit).ToArray();
        var nextCursor = candidates.Length > limit ? EncodeCursor(items[^1].Id) : null;
        return Task.FromResult(new VaultCandidatePage(items, nextCursor));
    }

    private static string EncodeCursor(string id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(CursorPrefix + id))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string? DecodeCursor(string? cursor)
    {
        if (cursor is null) return null;
        if (string.IsNullOrWhiteSpace(cursor) || cursor.Length > 512) throw new InvalidVaultCursorException();

        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            if (!value.StartsWith(CursorPrefix, StringComparison.Ordinal)) throw new InvalidVaultCursorException();
            var id = value[CursorPrefix.Length..];
            if (Candidates.All(candidate => candidate.Id != id)) throw new InvalidVaultCursorException();
            return id;
        }
        catch (InvalidVaultCursorException)
        {
            throw;
        }
        catch (FormatException exception)
        {
            throw new InvalidVaultCursorException(exception);
        }
    }
}
