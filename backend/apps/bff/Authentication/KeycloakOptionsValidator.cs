using Nodra.Bff.Configuration;

namespace Nodra.Bff.Authentication;

/// <summary>
/// Valida as configurações do cliente OIDC e do realm Keycloak usado pelo BFF.
/// </summary>
public sealed class KeycloakOptionsValidator(
    IHostEnvironment environment,
    IOptions<BffRuntimeOptions> runtimeOptions) : IValidateOptions<KeycloakOptions>
{
    /// <summary>
    /// Verifica a autoridade, o realm, o transporte, as credenciais e os callbacks OIDC.
    /// </summary>
    /// <param name="name">Nome da instância de opções validada.</param>
    /// <param name="options">Opções do Keycloak a validar.</param>
    /// <returns>Sucesso quando as opções são válidas; caso contrário, os erros de validação.</returns>
    public ValidateOptionsResult Validate(string? name, KeycloakOptions options)
    {
        if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out var authority))
        {
            return ValidateOptionsResult.Fail("Authentication:Keycloak:Authority deve ser uma URL absoluta.");
        }

        var authorityPath = authority.AbsolutePath.TrimEnd('/');
        if (!authorityPath.Contains("/realms/", StringComparison.OrdinalIgnoreCase)
            || authorityPath.EndsWith("/realms/master", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(authority.Query)
            || !string.IsNullOrEmpty(authority.Fragment))
        {
            return ValidateOptionsResult.Fail("O BFF deve usar um realm dedicado diferente de master.");
        }

        if (!string.Equals(authority.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !(environment.IsDevelopment() && (authority.IsLoopback || runtimeOptions.Value.AllowInsecureHttpForDevelopment)))
        {
            return ValidateOptionsResult.Fail("O issuer do Keycloak deve usar HTTPS, exceto em desenvolvimento local.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            return ValidateOptionsResult.Fail("ClientId e ClientSecret do BFF são obrigatórios.");
        }

        if (!IsLocalPath(options.CallbackPath) || !IsLocalPath(options.SignedOutCallbackPath))
        {
            return ValidateOptionsResult.Fail("Os callbacks OIDC devem ser paths locais absolutos.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsLocalPath(string path) =>
        path.Length > 0
        && path[0] == '/'
        && !path.StartsWith("//", StringComparison.Ordinal)
        && !path.Contains('\\');
}
