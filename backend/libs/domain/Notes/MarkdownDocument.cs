namespace Altivy.Backend.Domain.Notes;

/// <summary>
/// Representa os elementos estruturais extraídos de uma nota Markdown.
/// </summary>
public sealed record MarkdownDocument(
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Headings,
    IReadOnlyList<string> WikiLinks,
    IReadOnlyList<string> MarkdownLinks);