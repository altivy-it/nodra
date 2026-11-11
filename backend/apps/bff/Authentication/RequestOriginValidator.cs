using Microsoft.Extensions.Options;

namespace Nodra.Bff.Authentication;

/// <summary>
/// Verifica se uma origem HTTP está configurada como permitida para o BFF.
/// </summary>
public sealed class RequestOriginValidator(IOptions<BffSecurityOptions> securityOptions)
{
    private readonly HashSet<string> allowedOrigins = securityOptions.Value.AllowedOrigins
        .Select(Normalize)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Valida uma origem absoluta sem query ou fragmento contra a lista configurada.
    /// </summary>
    /// <param name="origin">Valor do header <c>Origin</c> da requisição.</param>
    /// <returns><c>true</c> quando a origem está permitida; caso contrário, <c>false</c>.</returns>
    public bool IsAllowed(string? origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var value)
            || !string.IsNullOrEmpty(value.Query)
            || !string.IsNullOrEmpty(value.Fragment)
            || value.AbsolutePath != "/")
        {
            return false;
        }

        return allowedOrigins.Contains(Normalize(value));
    }

    private static string Normalize(string origin) =>
        Normalize(new Uri(origin, UriKind.Absolute));

    private static string Normalize(Uri origin) =>
        origin.GetLeftPart(UriPartial.Authority);
}