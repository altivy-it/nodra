using System.Net;
using Altivy.Bff.Authentication;
using Altivy.Bff.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Altivy.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class ForwardedPrefixMiddlewareTests
{
    [Fact]
    public async Task AppliesNotesPrefixOnlyFromConfiguredProxyAndConsumesHeader()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
        context.Request.Headers["X-Forwarded-Prefix"] = "/notes";
        var middleware = new ForwardedPrefixMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new BffSecurityOptions { TrustedProxies = ["192.0.2.10"] }));

        await middleware.InvokeAsync(context);

        Assert.Equal("/notes", context.Request.PathBase);
        Assert.False(context.Request.Headers.ContainsKey("X-Forwarded-Prefix"));
    }

    [Fact]
    public async Task IgnoresPrefixFromUntrustedProxy()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.20");
        context.Request.Headers["X-Forwarded-Prefix"] = "/notes";
        var middleware = new ForwardedPrefixMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new BffSecurityOptions { TrustedProxies = ["192.0.2.10"] }));

        await middleware.InvokeAsync(context);

        Assert.True(context.Request.PathBase.HasValue is false);
        Assert.False(context.Request.Headers.ContainsKey("X-Forwarded-Prefix"));
    }
}