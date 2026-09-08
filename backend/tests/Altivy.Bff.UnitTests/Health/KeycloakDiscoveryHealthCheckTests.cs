using System.Net;
using Altivy.Bff.Authentication;
using Altivy.Bff.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Altivy.Bff.UnitTests.Health;

[Trait("Category", "Unit")]
public sealed class KeycloakDiscoveryHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsyncWithExpectedIssuerReturnsHealthy()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{\"issuer\":\"http://localhost:8080/realms/altivy\"}");
        using var client = new HttpClient(handler);
        var healthCheck = CreateHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsyncWithUnavailableDiscoveryReturnsUnhealthy()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "{}");
        using var client = new HttpClient(handler);
        var healthCheck = CreateHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsyncWithUnexpectedIssuerReturnsUnhealthy()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{\"issuer\":\"http://localhost:8080/realms/other\"}");
        using var client = new HttpClient(handler);
        var healthCheck = CreateHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsyncWithTimeoutReturnsUnhealthy()
    {
        var options = ValidOptions();
        using var handler = new TimeoutHttpMessageHandler();
        using var client = new HttpClient(handler);
        var healthCheck = new KeycloakDiscoveryHealthCheck(new StubHttpClientFactory(client), options);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    private static KeycloakDiscoveryHealthCheck CreateHealthCheck(HttpClient client) =>
        new(new StubHttpClientFactory(client), ValidOptions());

    private static IOptions<KeycloakOptions> ValidOptions() => Options.Create(new KeycloakOptions
    {
        Authority = "http://localhost:8080/realms/altivy",
        ClientId = "altivy-bff",
        ClientSecret = "synthetic-test-secret"
    });

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body)
            });
    }

    private sealed class TimeoutHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => throw new TaskCanceledException();
    }
}
