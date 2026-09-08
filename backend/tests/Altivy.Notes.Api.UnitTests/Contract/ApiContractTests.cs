using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Altivy.Backend.Application.Vaults;
using Altivy.Backend.Application.Notes;
using Altivy.Backend.Domain.Notes;
using Altivy.Backend.Domain.Vaults;
using Altivy.Notes.Api.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Altivy.Notes.Api.UnitTests.Contract;

[Trait("Category", "Integration")]
public sealed class ApiContractTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    public ApiContractTests(ApiFactory factory)
    {
        this.factory = factory;
        factory.Reset();
    }

    [Fact]
    public async Task GetVaultWithGrantReturnsPublicContract()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults/lab-0001");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"id\":\"lab-0001\"", body, StringComparison.Ordinal);
        Assert.Contains("\"scope\":\"vault:read\"", body, StringComparison.Ordinal);
        Assert.Equal("vault-lab-0001", factory.DecisionService.Resource);
        Assert.Equal("vault:read", factory.DecisionService.Scope);
    }

    [Fact]
    public async Task ListVaultsReturnsOnlyAuthorizedVaultsAcrossCandidatePages()
    {
        factory.VaultReader.SetCandidates(
        [
            new VaultCandidate("lab-0001", "Laboratório um"),
            new VaultCandidate("lab-0002", "Vault negado"),
            new VaultCandidate("lab-0003", "Laboratório três")
        ]);
        factory.DecisionService.SetDecision("vault-lab-0001", new AuthorizationDecision(true, true, 200));
        factory.DecisionService.SetDecision("vault-lab-0002", new AuthorizationDecision(false, true, 403));
        factory.DecisionService.SetDecision("vault-lab-0003", new AuthorizationDecision(true, true, 200));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults?limit=2");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("lab-0001", body, StringComparison.Ordinal);
        Assert.Contains("lab-0003", body, StringComparison.Ordinal);
        Assert.DoesNotContain("lab-0002", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Vault negado", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListVaultsWithUnavailableDecisionReturnsNoPartialPage()
    {
        factory.VaultReader.SetCandidates(
        [
            new VaultCandidate("lab-0001", "Laboratório um"),
            new VaultCandidate("lab-0002", "Vault indisponível")
        ]);
        factory.DecisionService.SetDecision("vault-lab-0001", new AuthorizationDecision(true, true, 200));
        factory.DecisionService.SetDecision("vault-lab-0002", new AuthorizationDecision(false, false, 503));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("lab-0001", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Vault indisponível", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListVaultsWithAllDecisionsDeniedReturnsEmptyPage()
    {
        factory.VaultReader.SetCandidates(
        [
            new VaultCandidate("lab-0001", "Vault negado um"),
            new VaultCandidate("lab-0002", "Vault negado dois")
        ]);
        factory.DecisionService.Decision = new AuthorizationDecision(false, true, 403);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"items\":[],\"nextCursor\":null}", body);
        Assert.DoesNotContain("Vault negado", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListVaultsWithoutBearerTokenReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/vaults");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.VaultReader.CallCount);
    }

    [Fact]
    public async Task ListVaultsWithInvalidCursorReturnsBadRequest()
    {
        factory.VaultReader.RejectCursors = true;
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults?cursor=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ListVaultsWithInvalidLimitReturnsBadRequest(int limit)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync($"/api/v1/vaults?limit={limit}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetVaultWithInvalidIdentifierReturnsProblemDetails()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults/lab_0001");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetVaultWithDeniedDecisionReturnsForbiddenProblemDetails()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(false, true, 403);
        factory.VaultReader.Reset();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults/lab-0001");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, factory.VaultReader.CallCount);
    }

    [Fact]
    public async Task GetVaultWithUnavailableDecisionReturnsServiceUnavailable()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(false, false, 503);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults/lab-0001");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetAuthorizedMissingVaultReturnsNotFound()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetVaultWithEmptyBearerTokenReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

        using var response = await client.GetAsync("/api/v1/vaults/lab-0001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.VaultReader.CallCount);
    }

    [Fact]
    public async Task GetHealthLiveReturnsOkWithoutAuthentication()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"ok\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetSessionWithAuthenticatedUserReturnsSubject()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/session");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"subject\":\"synthetic-user\"", body, StringComparison.Ordinal);
        Assert.Contains("\"authenticated\":true", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NotesCrudWithWriteGrantUsesEtagForMutations()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        factory.NoteStore.Reset();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var create = await client.PostAsJsonAsync("/api/v1/vaults/lab-0001/notes/projeto.md", new { content = "# Projeto" });
        using var get = await client.GetAsync("/api/v1/vaults/lab-0001/notes/projeto.md");
        var etag = get.Headers.ETag?.Tag;
        client.DefaultRequestHeaders.IfMatch.ParseAdd(etag);
        using var update = await client.PutAsJsonAsync("/api/v1/vaults/lab-0001/notes/projeto.md", new { content = "# Atualizado" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(etag));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        Assert.Equal(VaultAuthorization.WriteScope, factory.DecisionService.Scope);
    }

    [Fact]
    public async Task UpdateNoteWithoutIfMatchReturnsPreconditionRequired()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.PutAsJsonAsync("/api/v1/vaults/lab-0001/notes/projeto.md", new { content = "# Projeto" });

        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public FakeAuthorizationDecisionService DecisionService { get; } = new();

    public FakeVaultReader VaultReader { get; } = new();

    public FakeNoteStore NoteStore { get; } = new();

    public void Reset()
    {
        DecisionService.Reset();
        VaultReader.Reset();
        NoteStore.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAuthorizationDecisionService>();
            services.AddSingleton<IAuthorizationDecisionService>(DecisionService);
            services.RemoveAll<IVaultReader>();
            services.AddSingleton<IVaultReader>(VaultReader);
            services.RemoveAll<INoteStore>();
            services.AddSingleton<INoteStore>(NoteStore);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                "Test",
                _ => { });
        });
    }
}

public sealed class FakeNoteStore : INoteStore
{
    private readonly Dictionary<string, NoteDocument> notes = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<NoteDocument>> ListAsync(string vaultId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<NoteDocument>>(notes.Values.OrderBy(note => note.Path.Value).ToArray());

    public Task<NoteDocument?> FindAsync(string vaultId, NotePath path, CancellationToken cancellationToken) =>
        Task.FromResult(notes.GetValueOrDefault(path.Value));

    public Task<bool> TryCreateAsync(string vaultId, NotePath path, string content, CancellationToken cancellationToken)
    {
        if (notes.ContainsKey(path.Value)) return Task.FromResult(false);
        notes[path.Value] = new NoteDocument(path, content, "version-1");
        return Task.FromResult(true);
    }

    public Task<NoteWriteResult> UpdateAsync(string vaultId, NotePath path, string content, string expectedETag, CancellationToken cancellationToken)
    {
        if (!notes.TryGetValue(path.Value, out var note)) return Task.FromResult(NoteWriteResult.NotFound);
        if (note.ETag != expectedETag) return Task.FromResult(NoteWriteResult.Conflict);
        notes[path.Value] = note with { Content = content, ETag = "version-2" };
        return Task.FromResult(NoteWriteResult.Updated);
    }

    public Task<NoteWriteResult> DeleteAsync(string vaultId, NotePath path, string expectedETag, CancellationToken cancellationToken) =>
        Task.FromResult(notes.TryGetValue(path.Value, out var note) && note.ETag == expectedETag
            ? notes.Remove(path.Value) ? NoteWriteResult.Updated : NoteWriteResult.NotFound
            : notes.ContainsKey(path.Value) ? NoteWriteResult.Conflict : NoteWriteResult.NotFound);

    public void Reset() => notes.Clear();
}

public sealed class FakeVaultReader : IVaultReader
{
    private IReadOnlyList<VaultCandidate> candidates =
    [
        new("lab-0001", "Laboratório")
    ];

    public int CallCount { get; private set; }

    public bool RejectCursors { get; set; }

    public Task<Vault?> FindAsync(string id, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult<Vault?>(id == "lab-0001" ? Vault.Create(id, "Laboratório") : null);
    }

    public Task<VaultCandidatePage> ReadCandidatesAsync(
        string? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        if (RejectCursors && cursor is not null) throw new InvalidVaultCursorException();
        var offset = cursor is null ? 0 : int.Parse(cursor, System.Globalization.CultureInfo.InvariantCulture);
        var items = candidates.Skip(offset).Take(limit).ToArray();
        var nextOffset = offset + items.Length;
        var nextCursor = nextOffset < candidates.Count
            ? nextOffset.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : null;
        return Task.FromResult(new VaultCandidatePage(items, nextCursor));
    }

    public void SetCandidates(IReadOnlyList<VaultCandidate> value)
    {
        candidates = value;
        CallCount = 0;
    }

    public void Reset()
    {
        CallCount = 0;
        RejectCursors = false;
        candidates = [new VaultCandidate("lab-0001", "Laboratório")];
    }
}

public sealed class FakeAuthorizationDecisionService : IAuthorizationDecisionService
{
    private readonly Dictionary<string, AuthorizationDecision> decisions = new(StringComparer.Ordinal);

    public AuthorizationDecision Decision { get; set; } = new(true, true, 200);

    public string? Resource { get; private set; }

    public string? Scope { get; private set; }

    public Task<AuthorizationDecision> EvaluateAsync(
        string accessToken,
        string resource,
        string scope,
        CancellationToken cancellationToken)
    {
        Resource = resource;
        Scope = scope;
        return Task.FromResult(decisions.GetValueOrDefault(resource, Decision));
    }

    public void SetDecision(string resource, AuthorizationDecision decision) => decisions[resource] = decision;

    public void Reset()
    {
        Decision = new AuthorizationDecision(true, true, 200);
        decisions.Clear();
        Resource = null;
        Scope = null;
    }
}

#pragma warning disable CS0618
public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
            [new Claim("sub", "synthetic-user")],
            JwtBearerDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, JwtBearerDefaults.AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
#pragma warning restore CS0618
