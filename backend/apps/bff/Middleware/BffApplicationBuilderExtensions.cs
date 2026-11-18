using Nodra.Bff.Authentication;

namespace Nodra.Bff.Middleware;

/// <summary>
/// Fornece métodos para compor o pipeline HTTP do BFF.
/// </summary>
public static class BffApplicationBuilderExtensions
{
    /// <summary>
    /// Configura headers encaminhados, HTTPS, validação de origem, rate limiting, autenticação, autorização e antiforgery.
    /// </summary>
    /// <param name="app">Aplicação Web cujo pipeline HTTP será configurado.</param>
    /// <returns>A mesma aplicação Web, permitindo encadear outras extensões de pipeline.</returns>
    public static WebApplication UseBff(this WebApplication app)
    {
        _ = app.UseMiddleware<ForwardedPrefixMiddleware>();
        _ = app.UseForwardedHeaders();
        _ = app.UseHttpsRedirection();
        _ = app.UseWebSockets();
        _ = app.UseMiddleware<OriginValidationMiddleware>();
        _ = app.UseRateLimiter();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.UseAntiforgery();

        return app;
    }
}
