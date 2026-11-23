namespace Nodra.Bff.Authentication;

/// <summary>
/// Monta a URL do console de conta do Keycloak, onde o usuário gerencia perfil,
/// senha e autenticação. O Nodra não implementa gestão de conta.
/// </summary>
public static class KeycloakAccountConsole
{
    /// <summary>
    /// Retorna a URL do console de conta do realm configurado.
    /// </summary>
    /// <param name="authority">Authority OIDC do realm, por exemplo <c>https://host/realms/altivy</c>.</param>
    /// <param name="clientId">Client que aparece como origem; o Keycloak usa a <c>baseUrl</c> dele no link de retorno.</param>
    /// <returns>URL absoluta do console de conta.</returns>
    public static string BuildUrl(string authority, string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        return $"{authority.TrimEnd('/')}/account/?referrer={Uri.EscapeDataString(clientId)}";
    }
}
