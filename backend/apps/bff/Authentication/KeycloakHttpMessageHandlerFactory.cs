namespace Nodra.Bff.Authentication;

/// <summary>
/// Cria handlers HTTP para chamadas ao Keycloak com política de redirecionamento controlada.
/// </summary>
public static class KeycloakHttpMessageHandlerFactory
{
    /// <summary>
    /// Cria um handler HTTP configurado para a autoridade do Keycloak.
    /// </summary>
    /// <param name="authority">URL absoluta da autoridade do Keycloak.</param>
    /// <returns>Handler HTTP sem redirecionamento automático e com proxy adequado ao ambiente.</returns>
    public static HttpClientHandler Create(string authority)
    {
        var authorityUri = new Uri(authority, UriKind.Absolute);
        return new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseProxy = !authorityUri.IsLoopback
        };
    }
}
