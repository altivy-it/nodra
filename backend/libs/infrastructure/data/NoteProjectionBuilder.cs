using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nodra.Backend.Application.Notes;

namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Constrói a projeção reconstruível de uma nota canônica.
/// </summary>
public static class NoteProjectionBuilder
{
    /// <summary>
    /// Converte uma nota do filesystem em um registro pronto para persistência.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="note">Nota canônica lida do filesystem.</param>
    /// <param name="indexedAt">Instante UTC da projeção.</param>
    /// <returns>Registro sem o conteúdo integral da nota.</returns>
    public static NoteProjectionRecord Build(string vaultId, NoteDocument note, DateTimeOffset indexedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vaultId);
        ArgumentNullException.ThrowIfNull(note);

        var metadata = note.Metadata;
        _ = metadata.Properties.TryGetValue("title", out var title);

        return new NoteProjectionRecord
        {
            VaultId = vaultId,
            Path = note.Path.Value,
            ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(note.Content))),
            Title = title,
            PropertiesJson = JsonSerializer.Serialize(metadata.Properties),
            TagsJson = JsonSerializer.Serialize(metadata.Tags),
            HeadingsJson = JsonSerializer.Serialize(metadata.Headings),
            WikiLinksJson = JsonSerializer.Serialize(metadata.WikiLinks),
            MarkdownLinksJson = JsonSerializer.Serialize(metadata.MarkdownLinks),
            IndexedAt = indexedAt
        };
    }
}
