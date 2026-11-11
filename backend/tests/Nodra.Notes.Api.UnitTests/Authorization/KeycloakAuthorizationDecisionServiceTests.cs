using System.Net;
using System.Text;
using Nodra.Notes.Api.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Nodra.Notes.Api.UnitTests.Authorization;

[Trait("Category", "Unit")]
public sealed class KeycloakAuthorizationDecisionServiceTests
{
    [Fact]
    public async Task EvaluateAsyncWithGrantedDecisionReturnsAllowed()
    {
        using var handler = new StubHandler(HttpStatusCode.OK, "{\"result\":true}");
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var decision = await service.EvaluateAsync("access-token", "vault-lab-0001", "vault:read", CancellationToken.None);

        Assert.True(decision.Allowed);
        Assert.True(decision.Available);
        Assert.Equal(HttpStatusCode.OK, (HttpStatusCode)decision.StatusCode);
        Assert.Equal("Bearer access-token", handler.AuthorizationHeader);
        Assert.Equal("altivy-api", handler.RequestBody["audience"]);
        Assert.Equal("vault-lab-0001#vault:read", handler.RequestBody["permission"]);
    }

    [Fact]
    public async Task EvaluateAsyncWithDeniedDecisionReturnsForbidden()
    {
        using var handler = new StubHandler(HttpStatusCode.Forbidden, "{}");
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var decision = await service.EvaluateAsync("access-token", "vault-lab-0002", "vault:read", CancellationToken.None);

        Assert.False(decision.Allowed);
        Assert.True(decision.Available);
        Assert.Equal((int)HttpStatusCode.Forbidden, decision.StatusCode);
    }

    [Fact]
    public async Task EvaluateAsyncWithTransportFailureReturnsUnavailable()
    {
        using var handler = new FailingHandler(new HttpRequestException());
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var decision = await service.EvaluateAsync("access-token", "vault-lab-0001", "vault:read", CancellationToken.None);

        Assert.False(decision.Allowed);
        Assert.False(decision.Available);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, decision.StatusCode);
    }

    [Fact]
    public async Task EvaluateAsyncWithTimeoutReturnsUnavailable()
    {
        using var handler = new FailingHandler(new TaskCanceledException());
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var decision = await service.EvaluateAsync("access-token", "vault-lab-0001", "vault:read", CancellationToken.None);

        Assert.False(decision.Allowed);
        Assert.False(decision.Available);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, decision.StatusCode);
    }

    [Fact]
    public async Task EvaluateAsyncWithCorruptedJsonResponseReturnsUnavailable()
    {
        using var handler = new StubHandler(HttpStatusCode.OK, "invalid-json-content");
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var decision = await service.EvaluateAsync("access-token", "vault-lab-0001", "vault:read", CancellationToken.None);

        Assert.False(decision.Allowed);
        Assert.False(decision.Available);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, decision.StatusCode);
    }

    private static KeycloakAuthorizationDecisionService CreateService(HttpClient client)
    {
        return new KeycloakAuthorizationDecisionService(
            client,
            new KeycloakAuthorizationOptions
            {
                Authority = "http://localhost:8080/realms/altivy",
                Audience = "altivy-api"
            },
            NullLogger<KeycloakAuthorizationDecisionService>.Instance);
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public Dictionary<string, string> RequestBody { get; } = [];

        public string? AuthorizationHeader { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AuthorizationHeader = request.Headers.Authorization?.ToString();
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                RequestBody[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class FailingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => throw exception;
    }
}
