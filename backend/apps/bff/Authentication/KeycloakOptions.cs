namespace Nodra.Bff.Authentication;

/// <summary>
/// Define as configurações do cliente OIDC do BFF no Keycloak.
/// </summary>
public sealed class KeycloakOptions
{
    /// <summary>
    /// Nome da seção de configuração da integração com o Keycloak.
    /// </summary>
    public const string SectionName = "Authentication:Keycloak";

    /// <summary>
    /// URL absoluta do realm dedicado do Keycloak.
    /// </summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>
    /// Identificador do cliente OIDC do BFF.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Segredo do cliente OIDC do BFF.
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Path local usado pelo callback de login OIDC.
    /// </summary>
    public string CallbackPath { get; init; } = "/signin-oidc";

    /// <summary>
    /// Path local usado pelo callback de logout OIDC.
    /// </summary>
    public string SignedOutCallbackPath { get; init; } = "/signout-callback-oidc";
}