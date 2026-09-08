using Altivy.Bff.Authentication;
using Microsoft.AspNetCore.Builder;

namespace Altivy.Bff.Middleware;

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
        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseWebSockets();
        app.UseMiddleware<OriginValidationMiddleware>();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        return app;
    }
}