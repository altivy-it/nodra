using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Altivy.Bff.Authentication;

/// <summary>
/// Configura o cliente OpenID Connect do BFF para integração com o Keycloak.
/// </summary>
public sealed class ConfigureOpenIdConnectOptions(IOptions<KeycloakOptions> keycloakOptions)
    : IConfigureNamedOptions<OpenIdConnectOptions>
{
    /// <summary>
    /// Configura o esquema OpenID Connect padrão.
    /// </summary>
    /// <param name="options">Opções OpenID Connect que serão configuradas.</param>
    public void Configure(OpenIdConnectOptions options) =>
        Configure(OpenIdConnectDefaults.AuthenticationScheme, options);

    /// <summary>
    /// Configura o esquema OpenID Connect nomeado quando ele corresponde ao esquema do BFF.
    /// </summary>
    /// <param name="name">Nome do esquema a configurar.</param>
    /// <param name="options">Opções OpenID Connect que serão configuradas.</param>
    public void Configure(string? name, OpenIdConnectOptions options)
    {
        if (!string.Equals(name, OpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        var keycloak = keycloakOptions.Value;
        options.Authority = keycloak.Authority.TrimEnd('/');
        options.ClientId = keycloak.ClientId;
        options.ClientSecret = keycloak.ClientSecret;
        options.ResponseType = "code";
        options.ResponseMode = "query";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.MapInboundClaims = false;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.CallbackPath = keycloak.CallbackPath;
        options.SignedOutCallbackPath = keycloak.SignedOutCallbackPath;
        options.BackchannelHttpHandler = KeycloakHttpMessageHandlerFactory.Create(keycloak.Authority);
        options.RequireHttpsMetadata = Uri.TryCreate(keycloak.Authority, UriKind.Absolute, out var authority)
            && string.Equals(authority.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username",
            ValidateIssuer = true
        };
        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnTicketReceived = context =>
        {
            var properties = context.Properties;
            if (properties is null || !properties.Items.ContainsKey(ReauthenticationEvidence.ChallengeItem))
            {
                return Task.CompletedTask;
            }

            if (context.Principal is null ||
                !ReauthenticationEvidence.TryCreate(context.Principal, out var evidence) || evidence is null)
            {
                context.Fail("O retorno OIDC não contém auth_time válido para reautenticação.");
                return Task.CompletedTask;
            }

            properties.Items[ReauthenticationEvidence.AuthenticatedAtItem] =
                evidence.AuthenticatedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            properties.Items.Remove(ReauthenticationEvidence.ChallengeItem);
            return Task.CompletedTask;
        };
    }
}
