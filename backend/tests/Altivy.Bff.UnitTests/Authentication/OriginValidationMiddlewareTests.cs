using System.Net.WebSockets;
using Altivy.Bff.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Altivy.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class OriginValidationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncWithAllowedOriginContinuesPipeline()
    {
        var nextCalled = false;
        var middleware = new OriginValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreatePostContext("https://altivy.example");

        await middleware.InvokeAsync(context, CreateValidator());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsyncWithoutOriginReturnsForbidden()
    {
        var middleware = new OriginValidationMiddleware(_ => Task.CompletedTask);
        var context = CreatePostContext(null);

        await middleware.InvokeAsync(context, CreateValidator());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsyncWithExternalOriginReturnsForbidden()
    {
        var middleware = new OriginValidationMiddleware(_ => Task.CompletedTask);
        var context = CreatePostContext("https://attacker.example");

        await middleware.InvokeAsync(context, CreateValidator());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsyncWebSocketUpgradeWithoutOriginReturnsForbidden()
    {
        var middleware = new OriginValidationMiddleware(_ => Task.CompletedTask);
        var context = CreateWebSocketUpgradeContext(null);

        await middleware.InvokeAsync(context, CreateValidator());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsyncWebSocketUpgradeWithAllowedOriginContinuesPipeline()
    {
        var nextCalled = false;
        var middleware = new OriginValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateWebSocketUpgradeContext("https://altivy.example");

        await middleware.InvokeAsync(context, CreateValidator());

        Assert.True(nextCalled);
    }

    private static DefaultHttpContext CreatePostContext(string? origin)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }

        return context;
    }

    private static DefaultHttpContext CreateWebSocketUpgradeContext(string? origin)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Connection = "Upgrade";
        context.Request.Headers.Upgrade = "websocket";
        context.Features.Set<IHttpWebSocketFeature>(new TestWebSocketFeature());
        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }

        return context;
    }

    private sealed class TestWebSocketFeature : IHttpWebSocketFeature
    {
        public bool IsWebSocketRequest => true;

        public IList<string> WebSocketRequestedProtocols { get; } = [];

        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context) =>
            throw new NotSupportedException();
    }

    private static RequestOriginValidator CreateValidator() => new(Options.Create(new BffSecurityOptions
    {
        DataProtectionKeysPath = "synthetic-test-path",
        AllowedOrigins = ["https://altivy.example"]
    }));
}
