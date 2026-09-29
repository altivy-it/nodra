namespace Altivy.Backend.Domain.Notes;

/// <summary>
/// Representa os elementos estruturais extraídos de uma nota Markdown.
/// </summary>
public sealed record MarkdownDocument(
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Headings,
    IReadOnlyList<string> WikiLinks,
    IReadOnlyList<string> MarkdownLinks)
{
    private static readonly HashSet<string> Classifications = new(StringComparer.Ordinal)
    {
        "unclassified", "public", "internal", "restricted", "confidential"
    };

    private static readonly HashSet<string> DocumentTypes = new(StringComparer.Ordinal)
    {
        "note", "reference", "decision", "meeting", "runbook", "task"
    };

    /// <summary>
    /// Indica ausência de classificação válida ou uso explícito do valor padrão.
    /// </summary>
    public bool IsUnclassified =>
        !Properties.TryGetValue("altivy.classification", out var classification) ||
        !Classifications.Contains(classification) ||
        string.Equals(classification, "unclassified", StringComparison.Ordinal);

    /// <summary>
    /// Indica presença da versão, classificação, tipo e ao menos uma tag válidos.
    /// </summary>
    public bool HasMinimumMetadata =>
        Properties.TryGetValue("altivy.metadata_version", out var version) && version == "1.0" &&
        Properties.TryGetValue("altivy.classification", out var classification) && Classifications.Contains(classification) &&
        Properties.TryGetValue("altivy.doc_type", out var documentType) && DocumentTypes.Contains(documentType) &&
        Properties.ContainsKey("altivy.tags") && Tags.Count > 0;

    /// <summary>
    /// Indica se a nota contém referências locais que não resolvem para um caminho conhecido do vault.
    /// </summary>
    public bool HasUnresolvedLinks(string currentPath, IReadOnlySet<string> knownPaths)
    {
        ArgumentNullException.ThrowIfNull(currentPath);
        ArgumentNullException.ThrowIfNull(knownPaths);

        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return false;
        }

        var normalizedCurrent = NormalizeVaultPath(currentPath);
        var normalizedKnownPaths = knownPaths
            .Select(NormalizeVaultPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (normalizedKnownPaths.Count == 0)
        {
            return false;
        }

        foreach (var candidate in WikiLinks.Concat(MarkdownLinks))
        {
            var resolved = false;
            foreach (var possibleTarget in GetLinkCandidates(normalizedCurrent, candidate))
            {
                if (normalizedKnownPaths.Contains(possibleTarget))
                {
                    resolved = true;
                    break;
                }
            }

            if (!resolved)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetLinkCandidates(string currentPath, string rawTarget)
    {
        var target = NormalizeVaultPath(rawTarget);
        if (string.IsNullOrWhiteSpace(target) ||
            target.StartsWith('#') ||
            target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            target.Contains("://", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var targetWithoutFragment = target.Split('#', 2)[0].Trim();
        if (string.IsNullOrWhiteSpace(targetWithoutFragment))
        {
            yield break;
        }

        var normalizedTarget = NormalizeVaultPath(targetWithoutFragment);
        var currentDirectory = GetCurrentDirectory(currentPath);
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            normalizedTarget,
            EnsureMarkdownExtension(normalizedTarget)
        };

        if (!string.IsNullOrWhiteSpace(currentDirectory))
        {
            var relative = CombineRelativePath(currentDirectory, normalizedTarget);
            candidates.Add(relative);
            candidates.Add(EnsureMarkdownExtension(relative));
        }

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                yield return NormalizeVaultPath(candidate);
            }
        }
    }

    private static string CombineRelativePath(string basePath, string target)
    {
        var normalizedBase = NormalizeVaultPath(basePath).TrimEnd('/');
        var normalizedTarget = NormalizeVaultPath(target).TrimStart('/');

        if (string.IsNullOrWhiteSpace(normalizedTarget))
        {
            return normalizedBase;
        }

        var combined = string.IsNullOrWhiteSpace(normalizedBase)
            ? normalizedTarget
            : $"{normalizedBase}/{normalizedTarget}";

        return NormalizeVaultPath(combined);
    }

    private static string EnsureMarkdownExtension(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var normalized = NormalizeVaultPath(value);
        return normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"{normalized}.md";
    }

    private static string NormalizeVaultPath(string value)
    {
        var normalized = value.Replace('\\', '/').Trim();
        normalized = normalized.TrimStart('/');
        normalized = normalized.Split('?', 2)[0];
        normalized = normalized.Split('#', 2)[0];
        return normalized.Trim();
    }

    private static string GetCurrentDirectory(string currentPath)
    {
        var normalizedCurrent = NormalizeVaultPath(currentPath);
        var separatorIndex = normalizedCurrent.LastIndexOf('/');
        return separatorIndex < 0 ? string.Empty : normalizedCurrent[..separatorIndex];
    }
}