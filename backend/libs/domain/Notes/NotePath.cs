namespace Altivy.Backend.Domain.Notes;

/// <summary>
/// Representa o caminho relativo e seguro de uma nota Markdown dentro de um vault.
/// </summary>
public sealed class NotePath
{
    private NotePath(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Obtém o caminho relativo normalizado, usando barra como separador.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Cria um caminho de nota depois de validar as invariantes de segurança e formato.
    /// </summary>
    /// <param name="value">Caminho relativo da nota no vault.</param>
    /// <returns>O caminho de nota validado.</returns>
    /// <exception cref="ArgumentException">Quando o caminho não é uma nota Markdown relativa segura.</exception>
    public static NotePath Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 1024)
        {
            throw new ArgumentException("O caminho da nota é inválido.", nameof(value));
        }

        var normalizedValue = value.Replace('\\', '/').Trim();
        if (Path.IsPathRooted(normalizedValue) ||
            !normalizedValue.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            normalizedValue.Split('/').Any(segment =>
                string.IsNullOrWhiteSpace(segment) || segment is "." or ".."))
        {
            throw new ArgumentException("O caminho da nota é inválido.", nameof(value));
        }

        return new NotePath(normalizedValue);
    }
}