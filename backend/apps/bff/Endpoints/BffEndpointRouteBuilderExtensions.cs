using Altivy.Bff.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Altivy.Bff.Relay;

namespace Altivy.Bff.Endpoints;

/// <summary>
/// Fornece métodos para registrar os endpoints públicos do BFF.
/// </summary>
public static class BffEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Mapeia health checks e endpoints de login, antiforgery, sessão e logout do BFF.
    /// </summary>
    /// <param name="endpoints">Construtor de rotas que receberá os endpoints do BFF.</param>
    /// <returns>O mesmo construtor de rotas, permitindo encadear outros mapeamentos.</returns>
    public static IEndpointRouteBuilder MapBffEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        }).AllowAnonymous();
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready")
        }).AllowAnonymous();

        endpoints.MapGet("/bff/login", (string? returnUrl) =>
        {
            var redirectUri = LocalRedirect.Normalize(returnUrl);
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                [OpenIdConnectDefaults.AuthenticationScheme]);
        }).AllowAnonymous().RequireRateLimiting("login");

        endpoints.MapGet("/bff/antiforgery", (IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { requestToken = tokens.RequestToken });
        }).RequireAuthorization();

        endpoints.MapGet("/bff/session", (HttpContext context) =>
            Results.Ok(BffSessionResponse.FromPrincipal(context.User)))
            .AllowAnonymous();

        endpoints.MapPost("/bff/logout", async (IAntiforgery antiforgery, HttpContext context) =>
        {
            await antiforgery.ValidateRequestAsync(context);

            return Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        }).RequireAuthorization();

        endpoints.MapBffApiProxy();
        endpoints.MapBffHubsProxy();

        return endpoints;
    }
}