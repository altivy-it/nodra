using System.Net;
using System.Security.Claims;
using System.Text;
using System.Globalization;
using System.Diagnostics.Metrics;
using Altivy.Bff.Authentication;
using Altivy.Bff.Observability;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Altivy.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class TokenRefreshTests : IDisposable
{
    private readonly BffTelemetry telemetry = new();

    [Fact]
    public async Task ValidatePrincipalKeepsTokenOutsideRefreshWindow()
    {
        var now = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "current-access" },
            new AuthenticationToken { Name = "refresh_token", Value = "current-refresh" },
            new AuthenticationToken { Name = "expires_at", Value = now.AddMinutes(2).ToString("O") }
        ]);
        var context = CreateContext(properties);
        var coordinator = new StubRefreshCoordinator();
        var events = new CookieTokenRefreshEvents(
            coordinator,
            new StubTimeProvider(now),
            telemetry,
            NullLogger<CookieTokenRefreshEvents>.Instance);

        await events.ValidatePrincipal(context);

        Assert.False(context.ShouldRenew);
        Assert.Equal(0, coordinator.CallCount);
        Assert.Equal("current-access", context.Properties.GetTokenValue("access_token"));
    }

    [Fact]
    public async Task ValidatePrincipalRenewsExpiringTokensInServerTicket()
    {
        var now = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "old-access" },
            new AuthenticationToken { Name = "refresh_token", Value = "old-refresh" },
            new AuthenticationToken { Name = "expires_at", Value = now.AddSeconds(30).ToString("O") }
        ]);
        var context = CreateContext(properties);
        var coordinator = new StubRefreshCoordinator();
        var events = new CookieTokenRefreshEvents(
            coordinator,
            new StubTimeProvider(now),
            telemetry,
            NullLogger<CookieTokenRefreshEvents>.Instance);

        await events.ValidatePrincipal(context);

        Assert.True(context.ShouldRenew);
        Assert.Equal(1, coordinator.CallCount);
        Assert.Equal("new-access", context.Properties.GetTokenValue("access_token"));
        Assert.Equal("new-refresh", context.Properties.GetTokenValue("refresh_token"));
        Assert.Equal(
            now.AddMinutes(5),
            DateTimeOffset.Parse(
                context.Properties.GetTokenValue("expires_at")!,
                CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ValidatePrincipalWithoutRefreshTokenRejectsSession()
    {
        var now = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "expired-access" },
                new AuthenticationToken { Name = "expires_at", Value = now.AddMinutes(-1).ToString("O") }
        ]);
        var authentication = new RecordingAuthenticationService();
        var context = CreateContext(properties, authentication);
        var events = new CookieTokenRefreshEvents(
            new StubRefreshCoordinator(),
            new StubTimeProvider(now),
            telemetry,
            NullLogger<CookieTokenRefreshEvents>.Instance);

        await events.ValidatePrincipal(context);

        Assert.Null(context.Principal);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authentication.SignedOutScheme);
    }

    [Fact]
    public async Task ValidatePrincipalKeepsStillValidSessionOnTransientRefreshFailure()
    {
        var now = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        var properties = CreateRefreshableProperties(now.AddSeconds(30));
        var authentication = new RecordingAuthenticationService();
        var context = CreateContext(properties, authentication);
        var events = new CookieTokenRefreshEvents(
            new FailingRefreshCoordinator(new HttpRequestException("Falha transitória sintética.")),
            new StubTimeProvider(now),
            telemetry,
            NullLogger<CookieTokenRefreshEvents>.Instance);

        await events.ValidatePrincipal(context);

        Assert.NotNull(context.Principal);
        Assert.Null(authentication.SignedOutScheme);
    }

    [Fact]
    public async Task ValidatePrincipalKeepsStillValidSessionOnKeycloakTimeout()
    {
        var now = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        var context = CreateContext(CreateRefreshableProperties(now.AddSeconds(30)));
        var events = new CookieTokenRefreshEvents(
            new FailingRefreshCoordinator(new TaskCanceledException("Timeout sintético.")),
            new StubTimeProvider(now),
            telemetry,
            NullLogger<CookieTokenRefreshEvents>.Instance);

        await events.ValidatePrincipal(context);

        Assert.NotNull(context.Principal);
    }

    [Fact]
    public async Task ValidatePrincipalPropagatesRequestCancellation()
    {
        var now = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var context = CreateContext(CreateRefreshableProperties(now.AddSeconds(30)));
        context.HttpContext.RequestAborted = cancellation.Token;
        var events = new CookieTokenRefreshEvents(
            new FailingRefreshCoordinator(new TaskCanceledException("Cancelamento sintético.")),
            new StubTimeProvider(now),
            telemetry,
            NullLogger<CookieTokenRefreshEvents>.Instance);

        await Assert.ThrowsAsync<TaskCanceledException>(() => events.ValidatePrincipal(context));
    }

    [Fact]
    public async Task ValidatePrincipalRejectsExpiredSessionOnTransientRefreshFailureWithoutSigningOut()
    {
        var now = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        var properties = CreateRefreshableProperties(now.AddSeconds(-1));
        var authentication = new RecordingAuthenticationService();
        var context = CreateContext(properties, authentication);
        var events = new CookieTokenRefreshEvents(
            new FailingRefreshCoordinator(new HttpRequestException("Falha transitória sintética.")),
            new StubTimeProvider(now),
            telemetry,
            NullLogger<CookieTokenRefreshEvents>.Instance);

        await events.ValidatePrincipal(context);

        Assert.Null(context.Principal);
        Assert.Null(authentication.SignedOutScheme);
    }

    [Fact]
    public async Task ValidatePrincipalSignsOutSessionWhenRefreshIsPermanentlyRejected()
    {
        var now = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        var authentication = new RecordingAuthenticationService();
        var context = CreateContext(CreateRefreshableProperties(now.AddSeconds(30)), authentication);
        var events = new CookieTokenRefreshEvents(
            new FailingRefreshCoordinator(new TokenRefreshRejectedException(400)),
            new StubTimeProvider(now),
            telemetry,
            NullLogger<CookieTokenRefreshEvents>.Instance);

        await events.ValidatePrincipal(context);

        Assert.Null(context.Principal);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authentication.SignedOutScheme);
    }

    [Fact]
    public async Task RefreshAsyncCoalescesConcurrentRequestsForSameRefreshToken()
    {
        using var handler = new GatedTokenHandler();
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        var coordinator = new AccessTokenRefreshCoordinator(
            new StubHttpClientFactory(client),
            Options.Create(new KeycloakOptions
            {
                Authority = "https://credential.example/realms/altivy",
                ClientId = "altivy-bff",
                ClientSecret = "synthetic-test-secret"
            }),
            cache,
            TimeProvider.System);

        var first = coordinator.RefreshAsync("old-refresh", CancellationToken.None);
        await handler.Started.Task;
        var second = coordinator.RefreshAsync("old-refresh", CancellationToken.None);
        handler.Release.TrySetResult();
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, handler.CallCount);
        Assert.All(results, result => Assert.Equal("new-access", result.AccessToken));
        Assert.All(results, result => Assert.Equal("new-refresh", result.RefreshToken));
    }

    [Fact]
    public void RecordTokenRefreshPublishesOnlyFixedResultAndDuration()
    {
        using var listener = new MeterListener();
        var measurements = new List<(string Name, double Value, string? Result)>();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == BffTelemetry.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            measurements.Add((instrument.Name, value, GetResult(tags))));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            measurements.Add((instrument.Name, value, GetResult(tags))));
        listener.Start();

        telemetry.RecordTokenRefresh(TimeSpan.FromMilliseconds(250), TokenRefreshOutcome.Success);

        Assert.Contains(measurements, measurement =>
            measurement is ("altivy.bff.token_refresh.attempts", 1, "success"));
        Assert.Contains(measurements, measurement =>
            measurement is ("altivy.bff.token_refresh.duration", 0.25, "success"));
        Assert.All(measurements, measurement => Assert.Equal("success", measurement.Result));
    }

    public void Dispose() => telemetry.Dispose();

    private static string? GetResult(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == "result")
            {
                return tag.Value as string;
            }
        }

        return null;
    }

    private static CookieValidatePrincipalContext CreateContext(
        AuthenticationProperties properties,
        IAuthenticationService? authenticationService = null)
    {
        var httpContext = new DefaultHttpContext();
        if (authenticationService is not null)
        {
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(authenticationService)
                .BuildServiceProvider();
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "synthetic-user")],
            CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            properties,
            CookieAuthenticationDefaults.AuthenticationScheme);
        return new CookieValidatePrincipalContext(
            httpContext,
            new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme,
                null,
                typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            ticket);
    }

    private static AuthenticationProperties CreateRefreshableProperties(DateTimeOffset expiresAt)
    {
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "current-access" },
            new AuthenticationToken { Name = "refresh_token", Value = "current-refresh" },
            new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("O") }
        ]);
        return properties;
    }

    private sealed class StubRefreshCoordinator : IAccessTokenRefreshCoordinator
    {
        public int CallCount { get; private set; }

        public Task<AccessTokenRefreshResult> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new AccessTokenRefreshResult(
                "new-access",
                "new-refresh",
                new DateTimeOffset(2026, 8, 18, 12, 5, 0, TimeSpan.Zero)));
        }
    }

    private sealed class StubTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FailingRefreshCoordinator(Exception exception) : IAccessTokenRefreshCoordinator
    {
        public Task<AccessTokenRefreshResult> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken) => Task.FromException<AccessTokenRefreshResult>(exception);
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class GatedTokenHandler : HttpMessageHandler
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"access_token\":\"new-access\",\"refresh_token\":\"new-refresh\",\"expires_in\":300}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public string? SignedOutScheme { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            SignedOutScheme = scheme;
            return Task.CompletedTask;
        }
    }
}
