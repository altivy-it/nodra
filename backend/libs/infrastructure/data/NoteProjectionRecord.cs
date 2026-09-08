namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Projeção reconstruível dos metadados estruturais de uma nota.
/// </summary>
public sealed class NoteProjectionRecord
{
    public string VaultId { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string ContentHash { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string PropertiesJson { get; set; } = "{}";

    public string TagsJson { get; set; } = "[]";

    public string HeadingsJson { get; set; } = "[]";

    public string WikiLinksJson { get; set; } = "[]";

    public string MarkdownLinksJson { get; set; } = "[]";

    public DateTimeOffset IndexedAt { get; set; }
}