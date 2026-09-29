using Altivy.Bff.Authentication;
using Altivy.Bff.Configuration;
using Altivy.Bff.Health;
using Altivy.Bff.Observability;
using Altivy.Bff.Relay;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;

namespace Altivy.Bff.DependencyInjection;

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
        builder.Services.AddBffOpenTelemetry(builder.Configuration);

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
            services.AddSingleton(dataProtectionCertificate);
            dataProtection.ProtectKeysWithCertificate(dataProtectionCertificate);
        }

        services.AddMemoryCache(options => options.SizeLimit = runtimeOptions.MemoryCacheSizeLimit);
    }

    private static void AddCoreServices(IServiceCollection services)
    {
        services.AddSingleton<ServerSideTicketStore>();
        services.AddSingleton<RequestOriginValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAccessTokenRefreshCoordinator, AccessTokenRefreshCoordinator>();
        services.AddScoped<CookieTokenRefreshEvents>();
        services.AddHttpContextAccessor();
        services.AddTransient<IAccessTokenProvider, ServerAccessTokenProvider>();
        services.AddTransient<BearerTokenRelayHandler>();
        services.AddSingleton<IConfigureOptions<ForwardedHeadersOptions>, ConfigureForwardedHeadersOptions>();
        services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, ConfigureCookieOptions>();
        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureOpenIdConnectOptions>();
    }

    private static void AddAuthenticationAndAuthorization(IServiceCollection services)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
    }

    private static void AddRateLimiting(IServiceCollection services, BffRuntimeOptions runtimeOptions)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
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
        services
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
        services
            .AddHttpClient(KeycloakDiscoveryHealthCheck.HttpClientName, client =>
                client.Timeout = TimeSpan.FromSeconds(runtimeOptions.KeycloakDiscoveryTimeoutSeconds))
            .ConfigurePrimaryHttpMessageHandler(provider =>
            {
                var keycloak = provider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
                return KeycloakHttpMessageHandlerFactory.Create(keycloak.Authority);
            });
        services.AddHttpClient("AltivyApi", (provider, client) =>
            {
                var api = provider.GetRequiredService<IOptions<DownstreamApiOptions>>().Value;
                client.BaseAddress = new Uri(api.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(runtimeOptions.DownstreamApiTimeoutSeconds);
            })
            .AddHttpMessageHandler<BearerTokenRelayHandler>();
    }

    private static void AddHealthAndAntiforgery(IServiceCollection services, BffRuntimeOptions runtimeOptions)
    {
        services.AddHealthChecks()
            .AddCheck<KeycloakDiscoveryHealthCheck>("keycloak", tags: ["ready"]);
        services.AddAntiforgery(options =>
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