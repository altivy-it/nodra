using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Altivy.Backend.Domain.Notes;

/// <summary>
/// Extrai metadados determinísticos sem renderizar Markdown como HTML.
/// </summary>
public static partial class MarkdownDocumentParser
{
    private static readonly Regex HeadingLine = HeadingLineRegex();
    private static readonly Regex WikiLink = WikiLinkRegex();
    private static readonly Regex MarkdownLink = MarkdownLinkRegex();

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
        YamlMappingNode? frontmatter = null;
        if (content.StartsWith("---\n", StringComparison.Ordinal) || content.StartsWith("---\r\n", StringComparison.Ordinal))
        {
            var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            var closingLine = Array.IndexOf(lines, "---", 1);
            if (closingLine < 0)
            {
                throw new ArgumentException("O frontmatter da nota não foi encerrado.", nameof(content));
            }

            frontmatter = ParseFrontmatter(string.Join('\n', lines[1..closingLine]));
            AddProperties(frontmatter, properties, string.Empty);
            if (TryGetNode(frontmatter, "altivy", out var altivy) && altivy is YamlMappingNode altivyMapping)
            {
                AddProperties(altivyMapping, properties, "altivy.");
            }

            body = string.Join('\n', lines[(closingLine + 1)..]);
        }

        var tags = frontmatter is null ? [] : ExtractTags(frontmatter);
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

    /// <summary>
    /// Obtém somente o estado de classificação e metadados mínimos sem analisar o corpo Markdown.
    /// </summary>
    /// <param name="content">Conteúdo Markdown não confiável.</param>
    /// <returns>Estado derivado do frontmatter, sem alterar o conteúdo canônico.</returns>
    public static (bool IsUnclassified, bool HasMinimumMetadata) GetMetadataStatus(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.StartsWith("---\n", StringComparison.Ordinal) && !content.StartsWith("---\r\n", StringComparison.Ordinal))
        {
            return (true, false);
        }

        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var closingLine = Array.IndexOf(lines, "---", 1);
        if (closingLine < 0)
        {
            throw new ArgumentException("O frontmatter da nota não foi encerrado.", nameof(content));
        }

        var frontmatter = ParseFrontmatter(string.Join('\n', lines[1..closingLine]));
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddProperties(frontmatter, properties, string.Empty);
        if (TryGetNode(frontmatter, "altivy", out var altivy) && altivy is YamlMappingNode altivyMapping)
        {
            AddProperties(altivyMapping, properties, "altivy.");
        }

        var metadata = new MarkdownDocument(properties, ExtractTags(frontmatter), [], [], []);
        return (metadata.IsUnclassified, metadata.HasMinimumMetadata);
    }

    private static YamlMappingNode ParseFrontmatter(string yaml)
    {
        try
        {
            var stream = new YamlStream();
            using var reader = new StringReader(yaml);
            stream.Load(reader);
            if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode mapping)
            {
                throw new ArgumentException("O frontmatter deve conter um único objeto YAML.", nameof(yaml));
            }

            return mapping;
        }
        catch (YamlDotNet.Core.YamlException exception)
        {
            throw new ArgumentException("O frontmatter da nota é inválido.", nameof(yaml), exception);
        }
    }

    private static void AddProperties(
        YamlMappingNode mapping,
        Dictionary<string, string> properties,
        string prefix)
    {
        foreach (var (keyNode, valueNode) in mapping.Children)
        {
            if (keyNode is not YamlScalarNode { Value: { Length: > 0 } key })
            {
                continue;
            }

            string? value = valueNode switch
            {
                YamlScalarNode scalar => scalar.Value ?? string.Empty,
                YamlSequenceNode sequence => JsonSerializer.Serialize(sequence.Children
                    .OfType<YamlScalarNode>()
                    .Select(scalar => scalar.Value ?? string.Empty)
                    .ToArray()),
                _ => null
            };

            if (value is not null && !properties.TryAdd(prefix + key, value))
            {
                throw new ArgumentException("O frontmatter contém propriedades duplicadas.", nameof(mapping));
            }
        }
    }

    private static string[] ExtractTags(YamlMappingNode frontmatter)
    {
        if (TryGetNode(frontmatter, "altivy", out var altivy) &&
            altivy is YamlMappingNode altivyMapping &&
            TryGetNode(altivyMapping, "tags", out var namespacedTags))
        {
            return ReadTags(namespacedTags);
        }

        return TryGetNode(frontmatter, "tags", out var legacyTags) ? ReadTags(legacyTags) : [];
    }

    private static string[] ReadTags(YamlNode node)
    {
        var values = node switch
        {
            YamlScalarNode scalar => (scalar.Value ?? string.Empty)
                .Trim('[', ']')
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => value.Trim('"', '\'')),
            YamlSequenceNode sequence => sequence.Children
                .OfType<YamlScalarNode>()
                .Select(scalar => scalar.Value ?? string.Empty),
            _ => []
        };

        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryGetNode(YamlMappingNode mapping, string key, out YamlNode value)
    {
        foreach (var (keyNode, valueNode) in mapping.Children)
        {
            if (keyNode is YamlScalarNode scalar && string.Equals(scalar.Value, key, StringComparison.OrdinalIgnoreCase))
            {
                value = valueNode;
                return true;
            }
        }

        value = null!;
        return false;
    }

    [GeneratedRegex("^\\s{0,3}#{1,6}\\s+(?<text>.+?)\\s*#*\\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex HeadingLineRegex();

    [GeneratedRegex("\\[\\[(?<target>[^\\]|#]+)(?:#[^\\]|]+)?(?:\\|[^\\]]+)?\\]\\]", RegexOptions.CultureInvariant)]
    private static partial Regex WikiLinkRegex();

    [GeneratedRegex("(?<!!)\\[[^\\]]+\\]\\((?<target>[^)]+)\\)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownLinkRegex();
}