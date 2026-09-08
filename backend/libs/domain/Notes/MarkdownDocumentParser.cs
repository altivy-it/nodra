using System.Text.RegularExpressions;

namespace Altivy.Backend.Domain.Notes;

/// <summary>
/// Extrai metadados determinísticos sem renderizar Markdown como HTML.
/// </summary>
public static partial class MarkdownDocumentParser
{
    private static readonly Regex PropertyLine = PropertyLineRegex();
    private static readonly Regex HeadingLine = HeadingLineRegex();
    private static readonly Regex WikiLink = WikiLinkRegex();
    private static readonly Regex MarkdownLink = MarkdownLinkRegex();
    private static readonly Regex PropertyName = PropertyNameRegex();

    /// <summary>
    /// Analisa frontmatter e referências estruturais da nota.
    /// </summary>
    /// <param name="content">Conteúdo Markdown não confiável.</param>
    /// <returns>Elementos extraídos sem executar ou renderizar o conteúdo.</returns>
    /// <exception cref="ArgumentException">Quando o frontmatter é inválido.</exception>
    public static MarkdownDocument Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var body = content;
        if (content.StartsWith("---\n", StringComparison.Ordinal) || content.StartsWith("---\r\n", StringComparison.Ordinal))
        {
            var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            var closingLine = Array.IndexOf(lines, "---", 1);
            if (closingLine < 0)
            {
                throw new ArgumentException("O frontmatter da nota não foi encerrado.", nameof(content));
            }

            for (var index = 1; index < closingLine; index++)
            {
                var line = lines[index];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var match = PropertyLine.Match(line);
                if (!match.Success || !PropertyName.IsMatch(match.Groups["name"].Value))
                {
                    throw new ArgumentException("O frontmatter contém uma propriedade inválida.", nameof(content));
                }

                var name = match.Groups["name"].Value;
                if (!properties.TryAdd(name, NormalizeValue(match.Groups["value"].Value)))
                {
                    throw new ArgumentException("O frontmatter contém propriedades duplicadas.", nameof(content));
                }
            }

            body = string.Join('\n', lines[(closingLine + 1)..]);
        }

        var tags = ExtractTags(properties);
        var headings = HeadingLine.Matches(body)
            .Select(match => match.Groups["text"].Value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var wikiLinks = WikiLink.Matches(body)
            .Select(match => match.Groups["target"].Value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var markdownLinks = MarkdownLink.Matches(body)
            .Select(match => match.Groups["target"].Value.Trim())
            .Where(value => value.Length > 0 && !value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new MarkdownDocument(properties, tags, headings, wikiLinks, markdownLinks);
    }

    private static string[] ExtractTags(Dictionary<string, string> properties)
    {
        if (!properties.TryGetValue("tags", out var value))
        {
            return [];
        }

        return value.Trim('[', ']')
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tag => tag.Trim('"', '\''))
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeValue(string value)
    {
        var normalized = value.Trim();
        if ((normalized.StartsWith('"') && normalized.EndsWith('"')) ||
            (normalized.StartsWith('\'') && normalized.EndsWith('\'')))
        {
            return normalized[1..^1];
        }

        return normalized;
    }

    [GeneratedRegex("^(?<name>[A-Za-z][A-Za-z0-9_.-]{0,63})\\s*:\\s*(?<value>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex PropertyLineRegex();

    [GeneratedRegex("^\\s{0,3}#{1,6}\\s+(?<text>.+?)\\s*#*\\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex HeadingLineRegex();

    [GeneratedRegex("\\[\\[(?<target>[^\\]|#]+)(?:#[^\\]|]+)?(?:\\|[^\\]]+)?\\]\\]", RegexOptions.CultureInvariant)]
    private static partial Regex WikiLinkRegex();

    [GeneratedRegex("(?<!!)\\[[^\\]]+\\]\\((?<target>[^)]+)\\)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_.-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex PropertyNameRegex();
}