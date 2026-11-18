using Microsoft.AspNetCore.Authentication.Cookies;

namespace Nodra.Bff.Authentication;

/// <summary>
/// Configura o cookie de sessão e o armazenamento server-side da autenticação.
/// </summary>
public sealed class ConfigureCookieOptions(ServerSideTicketStore ticketStore)
    : IPostConfigureOptions<CookieAuthenticationOptions>
{
    /// <summary>
    /// Aplica as configurações de segurança e os eventos do cookie de autenticação.
    /// </summary>
    /// <param name="name">Nome do esquema de autenticação configurado.</param>
    /// <param name="options">Opções do cookie que serão configuradas.</param>
    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        if (!string.Equals(name, CookieAuthenticationDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        options.Cookie.Name = "__Host-Altivy.Session";
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.SessionStore = ticketStore;
        options.EventsType = typeof(CookieTokenRefreshEvents);
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    }
}
