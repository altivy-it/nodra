using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Nodra.Bff.Authentication;
using Nodra.Bff.Configuration;
using Nodra.Bff.Health;
using Nodra.Bff.Observability;
using Nodra.Bff.Relay;

namespace Nodra.Bff.DependencyInjection;

/// <summary>
/// Fornece métodos para registrar os serviços e integrações usados pelo BFF.
/// </summary>
public static class BffServiceCollectionExtensions
{
    /// <summary>
    /// Registra proteção de dados, autenticação, autorização, clientes HTTP, health checks e serviços do BFF no IoC.
    /// </summary>
    /// <param name="builder">Builder da aplicação cujo container de serviços será configurado.</param>
    /// <returns>O mesmo builder, permitindo encadear outras extensões de composição.</returns>
    public static WebApplicationBuilder AddBffServices(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddBffOpenTelemetry(builder.Configuration);

        var securityOptions = builder.Configuration
            .GetRequiredSection(BffSecurityOptions.SectionName)
            .Get<BffSecurityOptions>()
            ?? throw new InvalidOperationException("A configuração de segurança do BFF é obrigatória.");
        var runtimeOptions = builder.Configuration
            .GetSection(BffRuntimeOptions.SectionName)
            .Get<BffRuntimeOptions>()
            ?? new BffRuntimeOptions();

        AddDataProtectionServices(builder.Services, securityOptions, runtimeOptions);
        AddCoreServices(builder.Services);
        AddAuthenticationAndAuthorization(builder.Services);
        AddRateLimiting(builder.Services, runtimeOptions);
        AddDownstreamHttpClients(builder.Services, runtimeOptions);
        AddHealthAndAntiforgery(builder.Services, runtimeOptions);

        return builder;
    }

    private static void AddDataProtectionServices(
        IServiceCollection services,
        BffSecurityOptions securityOptions,
        BffRuntimeOptions runtimeOptions)
    {
        var dataProtection = services
            .AddDataProtection()
            .SetApplicationName(runtimeOptions.DataProtectionApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(securityOptions.DataProtectionKeysPath));
        if (!string.IsNullOrWhiteSpace(securityOptions.DataProtectionCertificatePath))
        {
            var dataProtectionCertificate = X509CertificateLoader.LoadPkcs12FromFile(
                securityOptions.DataProtectionCertificatePath,
                securityOptions.DataProtectionCertificatePassword,
                X509KeyStorageFlags.EphemeralKeySet);
            _ = services.AddSingleton(dataProtectionCertificate);
            _ = dataProtection.ProtectKeysWithCertificate(dataProtectionCertificate);
        }

        _ = services.AddMemoryCache(options => options.SizeLimit = runtimeOptions.MemoryCacheSizeLimit);
    }

    private static void AddCoreServices(IServiceCollection services)
    {
        _ = services.AddSingleton<ServerSideTicketStore>();
        _ = services.AddSingleton<RequestOriginValidator>();
        _ = services.AddSingleton(TimeProvider.System);
        _ = services.AddSingleton<IAccessTokenRefreshCoordinator, AccessTokenRefreshCoordinator>();
        _ = services.AddScoped<CookieTokenRefreshEvents>();
        _ = services.AddHttpContextAccessor();
        _ = services.AddTransient<IAccessTokenProvider, ServerAccessTokenProvider>();
        _ = services.AddTransient<BearerTokenRelayHandler>();
        _ = services.AddSingleton<IConfigureOptions<ForwardedHeadersOptions>, ConfigureForwardedHeadersOptions>();
        _ = services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, ConfigureCookieOptions>();
        _ = services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureOpenIdConnectOptions>();
    }

    private static void AddAuthenticationAndAuthorization(IServiceCollection services)
    {
        _ = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect();
        _ = services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
    }

    private static void AddRateLimiting(IServiceCollection services, BffRuntimeOptions runtimeOptions)
    {
        _ = services.AddRateLimiter(options =>
        {
            _ = options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                LoginRateLimitPartition.GetKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = runtimeOptions.LoginRateLimitPermitLimit,
                    Window = TimeSpan.FromSeconds(runtimeOptions.LoginRateLimitWindowSeconds),
                    QueueLimit = runtimeOptions.LoginRateLimitQueueLimit
                }));
        });
    }

    private static void AddDownstreamHttpClients(IServiceCollection services, BffRuntimeOptions runtimeOptions)
    {
        _ = services
            .AddHttpClient(AccessTokenRefreshCoordinator.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(runtimeOptions.TokenRefreshTimeoutSeconds);
                client.MaxResponseContentBufferSize = runtimeOptions.TokenRefreshMaxResponseContentBufferSize;
            })
            .ConfigurePrimaryHttpMessageHandler(provider =>
            {
                var keycloak = provider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
                return KeycloakHttpMessageHandlerFactory.Create(keycloak.Authority);
            });
        _ = services
            .AddHttpClient(KeycloakDiscoveryHealthCheck.HttpClientName, client =>
                client.Timeout = TimeSpan.FromSeconds(runtimeOptions.KeycloakDiscoveryTimeoutSeconds))
            .ConfigurePrimaryHttpMessageHandler(provider =>
            {
                var keycloak = provider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
                return KeycloakHttpMessageHandlerFactory.Create(keycloak.Authority);
            });
        _ = services.AddHttpClient("NodraApi", (provider, client) =>
            {
                var api = provider.GetRequiredService<IOptions<DownstreamApiOptions>>().Value;
                client.BaseAddress = new Uri(api.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(runtimeOptions.DownstreamApiTimeoutSeconds);
            })
            .AddHttpMessageHandler<BearerTokenRelayHandler>();
    }

    private static void AddHealthAndAntiforgery(IServiceCollection services, BffRuntimeOptions runtimeOptions)
    {
        _ = services.AddHealthChecks()
            .AddCheck<KeycloakDiscoveryHealthCheck>("keycloak", tags: ["ready"]);
        _ = services.AddAntiforgery(options =>
        {
            options.Cookie.Name = runtimeOptions.AntiforgeryCookieName;
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.HeaderName = runtimeOptions.AntiforgeryHeaderName;
        });
    }
}
