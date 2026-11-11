using System.Net;
using System.Net.Http.Headers;
using Nodra.Bff.Relay;

namespace Nodra.Bff.UnitTests.Relay;

[Trait("Category", "Unit")]
public sealed class BearerTokenRelayHandlerTests
{
    [Fact]
    public async Task SendAsyncReplacesBrowserAuthorizationWithServerToken()
    {
        using var capture = new CaptureHandler();
        using var relay = new BearerTokenRelayHandler(new StubAccessTokenProvider("server-token"))
        {
            InnerHandler = capture
        };
        using var client = new HttpClient(relay);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example/v1/vaults");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "browser-token");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bearer", capture.Authorization?.Scheme);
        Assert.Equal("server-token", capture.Authorization?.Parameter);
    }

    [Fact]
    public async Task SendAsyncWithoutServerTokenFailsClosed()
    {
        using var capture = new CaptureHandler();
        using var relay = new BearerTokenRelayHandler(new FailingAccessTokenProvider())
        {
            InnerHandler = capture
        };
        using var client = new HttpClient(relay);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => client.GetAsync("https://api.example/v1/vaults"));
    }

    private sealed class StubAccessTokenProvider(string accessToken) : IAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken) =>
            Task.FromResult(accessToken);
    }

    private sealed class FailingAccessTokenProvider : IAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken) =>
            Task.FromException<string>(new UnauthorizedAccessException("Token ausente."));
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
