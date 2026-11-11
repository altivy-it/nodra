namespace Nodra.Bff.Authentication;

/// <summary>
/// Normaliza URLs de retorno para impedir redirecionamentos externos.
/// </summary>
public static class LocalRedirect
{
    /// <summary>
    /// Retorna a URL local informada ou a raiz da aplicação quando ela não é segura.
    /// </summary>
    /// <param name="returnUrl">URL de retorno recebida da requisição.</param>
    /// <returns>URL local validada ou <c>/</c>.</returns>
    public static string Normalize(string? returnUrl) => Normalize(returnUrl, "/");

    public static string Normalize(string? returnUrl, string fallbackUrl) =>
        IsLocal(returnUrl) ? returnUrl! : fallbackUrl;

    private static bool IsLocal(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl)
        && returnUrl[0] == '/'
        && (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'));
}
