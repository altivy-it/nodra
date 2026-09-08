using System.Security.Claims;
using Altivy.Notes.Api.Authorization;
using Altivy.Notes.Api.Realtime;
using Altivy.Notes.Api.UnitTests.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace Altivy.Notes.Api.UnitTests.Realtime;

[Trait("Category", "Unit")]
public sealed class VaultHubTests
{
    [Fact]
    public async Task JoinVaultAsyncWithInvalidIdentifierThrowsHubException()
    {
        using var hub = CreateHub(new FakeAuthorizationDecisionService(), out _, authorizationHeader: "Bearer synthetic-token");

        await Assert.ThrowsAsync<HubException>(() => hub.JoinVaultAsync("invalid_id"));
    }

    [Fact]
    public async Task JoinVaultAsyncWithoutBearerTokenThrowsHubException()
    {
        using var hub = CreateHub(new FakeAuthorizationDecisionService(), out _, authorizationHeader: null);

        await Assert.ThrowsAsync<HubException>(() => hub.JoinVaultAsync("lab-0001"));
    }

    [Fact]
    public async Task JoinVaultAsyncWithDeniedDecisionThrowsHubException()
    {
        var decisionService = new FakeAuthorizationDecisionService
        {
            Decision = new AuthorizationDecision(false, true, 403)
        };
        using var hub = CreateHub(decisionService, out var groups, authorizationHeader: "Bearer synthetic-token");

        await Assert.ThrowsAsync<HubException>(() => hub.JoinVaultAsync("lab-0001"));
        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task JoinVaultAsyncWithGrantedDecisionAddsConnectionToVaultGroup()
    {
        var decisionService = new FakeAuthorizationDecisionService
        {
            Decision = new AuthorizationDecision(true, true, 200)
        };
        using var hub = CreateHub(decisionService, out var groups, authorizationHeader: "Bearer synthetic-token");

        await hub.JoinVaultAsync("lab-0001");

        Assert.Equal("vault-lab-0001", decisionService.Resource);
        Assert.Single(groups.Added);
        Assert.Equal("vault-lab-0001", groups.Added[0].GroupName);
        Assert.Equal("vault:read", decisionService.Scope);
    }

    private static VaultHub CreateHub(
        FakeAuthorizationDecisionService decisionService,
        out FakeGroupManager groups,
        string? authorizationHeader)
    {
        var httpContext = new DefaultHttpContext();
        if (authorizationHeader is not null)
        {
            httpContext.Request.Headers.Authorization = authorizationHeader;
        }

        var features = new FeatureCollection();
        features.Set<IHttpContextFeature>(new TestHttpContextFeature(httpContext));

        groups = new FakeGroupManager();
        return new VaultHub(decisionService)
        {
            Context = new FakeHubCallerContext(features),
            Groups = groups
        };
    }

    private sealed class TestHttpContextFeature(HttpContext? httpContext) : IHttpContextFeature
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }

    private sealed class FakeHubCallerContext(IFeatureCollection features) : HubCallerContext
    {
        public override string ConnectionId { get; } = "connection-1";

        public override string? UserIdentifier => null;

        public override ClaimsPrincipal User { get; } = new();

        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        public override IFeatureCollection Features { get; } = features;

        public override CancellationToken ConnectionAborted { get; } = CancellationToken.None;

        public override void Abort()
        {
        }
    }

    private sealed class FakeGroupManager : IGroupManager
    {
        public List<(string ConnectionId, string GroupName)> Added { get; } = [];

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Added.Add((connectionId, groupName));
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
