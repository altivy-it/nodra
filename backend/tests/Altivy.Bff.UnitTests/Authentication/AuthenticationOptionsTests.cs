using Altivy.Bff.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Altivy.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class AuthenticationOptionsTests
{
    [Fact]
    public void ConfigureOpenIdConnectUsesAuthorizationCodeWithPkceAndServerSavedTokens()
    {
        var keycloak = Options.Create(new KeycloakOptions
        {
            Authority = "https://credential.example/realms/altivy",
            ClientId = "altivy-bff",
            ClientSecret = "synthetic-test-secret"
        });
        var options = new OpenIdConnectOptions();

        new ConfigureOpenIdConnectOptions(keycloak)
            .Configure(OpenIdConnectDefaults.AuthenticationScheme, options);

        Assert.Equal("code", options.ResponseType);
        Assert.Equal("query", options.ResponseMode);
        Assert.True(options.UsePkce);
        Assert.True(options.SaveTokens);
        Assert.False(options.MapInboundClaims);
        Assert.True(options.RequireHttpsMetadata);
        var backchannelHandler = Assert.IsType<HttpClientHandler>(options.BackchannelHttpHandler);
        Assert.True(backchannelHandler.UseProxy);
        Assert.False(backchannelHandler.AllowAutoRedirect);
        Assert.Contains("openid", options.Scope);
        Assert.Contains("profile", options.Scope);
        Assert.Contains("email", options.Scope);
    }

    [Fact]
    public void ConfigureOpenIdConnectBypassesProxyOnlyForLoopbackAuthority()
    {
        var keycloak = Options.Create(new KeycloakOptions
        {
            Authority = "http://localhost:8080/realms/altivy",
            ClientId = "altivy-bff",
            ClientSecret = "synthetic-test-secret"
        });
        var options = new OpenIdConnectOptions();

        new ConfigureOpenIdConnectOptions(keycloak)
            .Configure(OpenIdConnectDefaults.AuthenticationScheme, options);

        var backchannelHandler = Assert.IsType<HttpClientHandler>(options.BackchannelHttpHandler);
        Assert.False(backchannelHandler.UseProxy);
        Assert.False(backchannelHandler.AllowAutoRedirect);
    }

    [Fact]
    public void ConfigureCookieUsesHostOnlySecureCookieAndServerSideStore()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new ServerSideTicketStore(cache);
        var options = new CookieAuthenticationOptions();

        new ConfigureCookieOptions(store)
            .PostConfigure(CookieAuthenticationDefaults.AuthenticationScheme, options);

        Assert.Equal("__Host-Altivy.Session", options.Cookie.Name);
        Assert.Equal("/", options.Cookie.Path);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.Same(store, options.SessionStore);
        Assert.Equal(typeof(CookieTokenRefreshEvents), options.EventsType);
        Assert.Equal(TimeSpan.FromHours(8), options.ExpireTimeSpan);
        Assert.False(options.SlidingExpiration);
    }
}