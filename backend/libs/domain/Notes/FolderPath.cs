namespace Nodra.Backend.Domain.Notes;

/// <summary>
/// Representa o caminho relativo e seguro de uma pasta dentro de um vault.
/// </summary>
public sealed class FolderPath
{
    private static readonly HashSet<string> s_reservedNames =
        ["con", "prn", "aux", "nul", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"];

    private FolderPath(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Obtém o caminho relativo normalizado, usando barra como separador.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Cria um caminho de pasta depois de validar suas invariantes de segurança e formato.
    /// </summary>
    /// <param name="value">Caminho relativo da pasta no vault.</param>
    /// <returns>O caminho de pasta validado.</returns>
    /// <exception cref="ArgumentException">Quando o caminho não é relativo e seguro.</exception>
    public static FolderPath Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 1024)
        {
            throw new ArgumentException("O caminho da pasta é inválido.", nameof(value));
        }

        var normalizedValue = value.Replace('\\', '/').Trim();
        if (Path.IsPathRooted(normalizedValue) ||
            normalizedValue.StartsWith("//", StringComparison.Ordinal) ||
            (normalizedValue.Length > 1 && char.IsAsciiLetter(normalizedValue[0]) && normalizedValue[1] == ':') ||
            normalizedValue.Split('/').Any(IsInvalidSegment))
        {
            throw new ArgumentException("O caminho da pasta é inválido.", nameof(value));
        }

        return new FolderPath(normalizedValue);
    }

    private static bool IsInvalidSegment(string segment)
    {
        var baseName = segment.Split('.')[0];
        return string.IsNullOrWhiteSpace(segment) ||
            segment is "." or ".." ||
            string.Equals(segment, ".altivy-trash", StringComparison.OrdinalIgnoreCase) ||
            segment.Any(character => char.IsControl(character) || "<>:\"|?*".Contains(character)) ||
            segment.EndsWith('.') ||
            segment.EndsWith(' ') ||
            s_reservedNames.Contains(baseName, StringComparer.OrdinalIgnoreCase);
    }
}
