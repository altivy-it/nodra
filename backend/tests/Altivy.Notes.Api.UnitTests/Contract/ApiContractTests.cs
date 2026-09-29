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
    public async Task TrashSettingsRoundTripUsesAuthenticatedSubject()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var update = await client.PutAsJsonAsync("/api/v1/me/trash-settings", new { retentionDays = 30 });
        using var get = await client.GetAsync("/api/v1/me/trash-settings");
        using var clear = await client.PutAsJsonAsync("/api/v1/me/trash-settings", new { retentionDays = (int?)null });

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        Assert.Equal("{\"retentionDays\":30}", await get.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NoContent, clear.StatusCode);
        Assert.Equal("synthetic-user", factory.RetentionStore.Subject);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3651)]
    public async Task TrashSettingsRejectsOutOfRangeValues(int retentionDays)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.PutAsJsonAsync("/api/v1/me/trash-settings", new { retentionDays });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(factory.RetentionStore.LastValue);
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
    public async Task ListFolderEntriesWithReadGrantReturnsImmediateEntries()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        factory.FolderStore.SetEntries(
            "projetos",
            [
                new VaultEntry("projetos/plano.md", "plano.md", false, "etag-1"),
                new VaultEntry("projetos/decisoes", "decisoes", true, null)
            ]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults/lab-0001/entries?parentPath=projetos");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"kind\":\"note\"", body, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"folder\"", body, StringComparison.Ordinal);
        Assert.Equal("vault:read", factory.DecisionService.Scope);
        Assert.Equal("projetos", factory.FolderStore.ParentPath);
    }

    [Fact]
    public async Task ListFolderEntriesWithDeniedGrantDoesNotReadTheStore()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(false, true, 403);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults/lab-0001/entries");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task ListFolderEntriesWithInvalidPathReturnsProblemDetails()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults/lab-0001/entries?parentPath=..%2Ffora");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task EnsureFolderWithWriteGrantIsIdempotent()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        factory.FolderStore.CreationResult = FolderCreationResult.Created;
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var created = await client.PutAsync("/api/v1/vaults/lab-0001/folders/projetos", null);
        factory.FolderStore.CreationResult = FolderCreationResult.AlreadyExists;
        using var existing = await client.PutAsync("/api/v1/vaults/lab-0001/folders/projetos", null);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/api/v1/vaults/lab-0001/folders/projetos", created.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.NoContent, existing.StatusCode);
        Assert.Equal("vault:write", factory.DecisionService.Scope);
        Assert.Equal(2, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task EnsureFolderWithDeniedGrantDoesNotAccessTheStore()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(false, true, 403);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.PutAsync("/api/v1/vaults/lab-0001/folders/projetos", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task EnsureFolderWithInvalidPathReturnsProblemDetails()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.PutAsync("/api/v1/vaults/lab-0001/folders/..%2Ffora", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task DeleteFolderWithDeleteGrantMovesEntryToTrash()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        factory.FolderStore.TrashMoveOutcome = new TrashMoveOutcome(
            TrashMoveResult.Moved,
            new TrashEntry(Guid.NewGuid(), "projetos", "projetos", true, DateTimeOffset.UtcNow, null, true));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.DeleteAsync("/api/v1/vaults/lab-0001/folders/projetos");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("vault:delete", factory.DecisionService.Scope);
        Assert.Equal(1, factory.FolderStore.CallCount);
        Assert.Equal("synthetic-user", factory.FolderStore.Subject);
    }

    [Fact]
    public async Task DeleteEmptyFolderWithDeniedGrantDoesNotAccessStore()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(false, true, 403);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.DeleteAsync("/api/v1/vaults/lab-0001/folders/projetos");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task DeleteNonEmptyFolderMovesToTrashInsteadOfConflicting()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        factory.FolderStore.TrashMoveOutcome = new TrashMoveOutcome(
            TrashMoveResult.Moved,
            new TrashEntry(Guid.NewGuid(), "projetos", "projetos", true, DateTimeOffset.UtcNow, null, true));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.DeleteAsync("/api/v1/vaults/lab-0001/folders/projetos");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task DeleteNoteRequiresIfMatchAndMovesToTrash()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        factory.FolderStore.TrashMoveOutcome = new TrashMoveOutcome(
            TrashMoveResult.Moved,
            new TrashEntry(Guid.NewGuid(), "projetos/plano.md", "plano.md", false, DateTimeOffset.UtcNow, null, true));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var missingPrecondition = await client.DeleteAsync("/api/v1/vaults/lab-0001/notes/projetos/plano.md");
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/vaults/lab-0001/notes/projetos/plano.md");
        request.Headers.IfMatch.ParseAdd("\"version-1\"");
        using var moved = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PreconditionRequired, missingPrecondition.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        Assert.Equal("vault:delete", factory.DecisionService.Scope);
        Assert.Equal("version-1", factory.FolderStore.ExpectedETag);
    }

    [Fact]
    public async Task ListTrashIsIsolatedToAuthenticatedSubjectAndRequiresReadGrant()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        factory.FolderStore.TrashEntries =
        [
            new TrashEntry(Guid.NewGuid(), "projetos/plano.md", "plano.md", false, DateTimeOffset.UtcNow, null, true)
        ];
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults/lab-0001/trash");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("projetos/plano.md", body, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic-user", body, StringComparison.Ordinal);
        Assert.Equal("vault:read", factory.DecisionService.Scope);
        Assert.Equal("synthetic-user", factory.FolderStore.Subject);
    }

    [Fact]
    public async Task ListTrashWithDeniedGrantDoesNotAccessStore()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(false, true, 403);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.GetAsync("/api/v1/vaults/lab-0001/trash");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task RestoreTrashUsesExplicitDestinationAndWriteGrant()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        factory.FolderStore.TrashRestoreOutcome = new TrashRestoreOutcome(TrashRestoreResult.Restored);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");
        var operationId = Guid.NewGuid();

        using var response = await client.PatchAsJsonAsync(
            $"/api/v1/vaults/lab-0001/trash/{operationId}", new { path = "arquivo/plano.md" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("vault:write", factory.DecisionService.Scope);
        Assert.Equal("arquivo/plano.md", factory.FolderStore.RestoreDestination);
        Assert.Equal(operationId, factory.FolderStore.RestoreOperationId);
    }

    [Fact]
    public async Task EmptyTrashWithoutRecentReauthenticationReturnsPreconditionRequired()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.DeleteAsync("/api/v1/vaults/lab-0001/trash");

        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
        Assert.Contains("reauthentication-required", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(0, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task EmptyTrashWithFreshReauthenticationRecordsAuditAndReturnsCounts()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        factory.FolderStore.EmptyTrashOutcome = new TrashEmptyOutcome(3, 1);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");
        client.DefaultRequestHeaders.Add("X-Altivy-Reauth-At", DateTimeOffset.UtcNow.AddSeconds(-30).ToString("O"));

        using var response = await client.DeleteAsync("/api/v1/vaults/lab-0001/trash");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("removedOperations", body, StringComparison.Ordinal);
        Assert.Equal("empty-trash-intent", factory.AuditStore.Events[0].Action);
        Assert.Equal("empty-trash-result", factory.AuditStore.Events[1].Action);
        Assert.Equal(1, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task MoveFolderWithWriteGrantReturnsNewPath()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.PatchAsJsonAsync(
            "/api/v1/vaults/lab-0001/folders/projetos", new { path = "arquivo/projetos" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("vault:write", factory.DecisionService.Scope);
        Assert.Equal(1, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task MoveFolderIntoItselfIsRejectedWithoutAccessingStore()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.PatchAsJsonAsync(
            "/api/v1/vaults/lab-0001/folders/projetos", new { path = "projetos/interno" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task MoveFolderToExistingDestinationReturnsCodedConflict()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        factory.FolderStore.MoveResult = FolderMoveResult.DestinationExists;
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.PatchAsJsonAsync(
            "/api/v1/vaults/lab-0001/folders/projetos", new { path = "arquivo" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("folder-destination-exists", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MoveFolderWithDeniedGrantDoesNotAccessStore()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(false, true, 403);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await client.PatchAsJsonAsync(
            "/api/v1/vaults/lab-0001/folders/projetos", new { path = "arquivo" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.FolderStore.CallCount);
    }

    [Fact]
    public async Task MoveNoteWithWriteGrantChangesPathAndRequiresCurrentEtag()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        await factory.Services.GetRequiredService<INoteStore>().TryCreateAsync(
            "lab-0001", NotePath.Create("rascunho.md"), "# Rascunho", CancellationToken.None);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var withoutEtag = await client.PatchAsJsonAsync(
            "/api/v1/vaults/lab-0001/notes/rascunho.md", new { path = "plano.md" });
        using var stale = await SendPatchAsync(client, "rascunho.md", "plano.md", "\"version-old\"");
        using var moved = await SendPatchAsync(client, "rascunho.md", "plano.md", "\"version-1\"");
        using var missing = await client.GetAsync("/api/v1/vaults/lab-0001/notes/rascunho.md");
        using var current = await client.GetAsync("/api/v1/vaults/lab-0001/notes/plano.md");

        Assert.Equal(HttpStatusCode.PreconditionRequired, withoutEtag.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Contains("note-version-conflict", await stale.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        Assert.Equal(VaultAuthorization.ReadScope, factory.DecisionService.Scope);
    }

    [Fact]
    public async Task MoveNoteToOccupiedDestinationReturnsConflictWithoutOverwriting()
    {
        factory.DecisionService.Decision = new AuthorizationDecision(true, true, 200);
        var store = factory.Services.GetRequiredService<INoteStore>();
        await store.TryCreateAsync("lab-0001", NotePath.Create("a.md"), "# A", CancellationToken.None);
        await store.TryCreateAsync("lab-0001", NotePath.Create("b.md"), "# B", CancellationToken.None);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await SendPatchAsync(client, "a.md", "b.md", "\"version-1\"");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("note-destination-exists", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal("# B", (await store.FindAsync("lab-0001", NotePath.Create("b.md"), CancellationToken.None))!.Content);
    }

    [Fact]
    public async Task MoveNoteWithDeniedGrantDoesNotMoveTheNote()
    {
        var store = factory.Services.GetRequiredService<INoteStore>();
        await store.TryCreateAsync("lab-0001", NotePath.Create("a.md"), "# A", CancellationToken.None);
        factory.DecisionService.Decision = new AuthorizationDecision(false, true, 403);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-token");

        using var response = await SendPatchAsync(client, "a.md", "b.md", "\"version-1\"");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotNull(await store.FindAsync("lab-0001", NotePath.Create("a.md"), CancellationToken.None));
    }

    private static async Task<HttpResponseMessage> SendPatchAsync(HttpClient client, string source, string destination, string etag)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/vaults/lab-0001/notes/{source}")
        {
            Content = JsonContent.Create(new { path = destination })
        };
        request.Headers.IfMatch.ParseAdd(etag);
        return await client.SendAsync(request);
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

    public FakeFolderStore FolderStore { get; } = new();

    public FakeTrashRetentionSettingsStore RetentionStore { get; } = new();

    public FakeTrashAuditStore AuditStore { get; } = new();

    public void Reset()
    {
        DecisionService.Reset();
        VaultReader.Reset();
        NoteStore.Reset();
        FolderStore.Reset();
        RetentionStore.Reset();
        AuditStore.Reset();
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
            services.RemoveAll<IFolderStore>();
            services.AddSingleton<IFolderStore>(FolderStore);
            services.RemoveAll<ITrashRetentionSettingsStore>();
            services.AddSingleton<ITrashRetentionSettingsStore>(RetentionStore);
            services.RemoveAll<ITrashAuditStore>();
            services.AddSingleton<ITrashAuditStore>(AuditStore);
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

public sealed class FakeFolderStore : IFolderStore
{
    private readonly Dictionary<string, IReadOnlyList<VaultEntry>> entries = new(StringComparer.Ordinal);

    public int CallCount { get; private set; }

    public string? ParentPath { get; private set; }

    public FolderCreationResult CreationResult { get; set; } = FolderCreationResult.Created;

    public FolderDeletionResult DeletionResult { get; set; } = FolderDeletionResult.NotFound;
    public TrashMoveOutcome TrashMoveOutcome { get; set; } = new(TrashMoveResult.NotFound);
    public TrashRestoreOutcome TrashRestoreOutcome { get; set; } = new(TrashRestoreResult.NotFound);
    public TrashEmptyOutcome EmptyTrashOutcome { get; set; } = new(0, 0);
    public IReadOnlyList<TrashEntry> TrashEntries { get; set; } = [];
    public string? Subject { get; private set; }
    public string? ExpectedETag { get; private set; }
    public string? RestoreDestination { get; private set; }
    public Guid RestoreOperationId { get; private set; }

    public Task<IReadOnlyList<VaultEntry>> ListChildrenAsync(
        string vaultId,
        FolderPath? parentPath,
        CancellationToken cancellationToken)
    {
        CallCount++;
        ParentPath = parentPath?.Value;
        return Task.FromResult(entries.GetValueOrDefault(ParentPath ?? string.Empty, []));
    }

    public Task<FolderCreationResult> EnsureCreatedAsync(
        string vaultId,
        FolderPath path,
        CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(CreationResult);
    }

    public Task<FolderDeletionResult> DeleteEmptyAsync(
        string vaultId,
        FolderPath path,
        CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(DeletionResult);
    }

    public FolderMoveResult MoveResult { get; set; } = FolderMoveResult.Moved;

    public Task<FolderMoveOutcome> MoveAsync(
        string vaultId,
        FolderPath source,
        FolderPath destination,
        CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(new FolderMoveOutcome(MoveResult, []));
    }

    public Task<TrashMoveOutcome> MoveToTrashAsync(
        string vaultId,
        string subject,
        FolderPath path,
        string? expectedETag,
        CancellationToken cancellationToken)
    {
        CallCount++;
        Subject = subject;
        ExpectedETag = expectedETag;
        return Task.FromResult(TrashMoveOutcome);
    }

    public Task<IReadOnlyList<TrashEntry>> ListTrashAsync(
        string vaultId,
        string subject,
        CancellationToken cancellationToken)
    {
        CallCount++;
        Subject = subject;
        return Task.FromResult(TrashEntries);
    }

    public Task<TrashRestoreOutcome> RestoreFromTrashAsync(
        string vaultId,
        string subject,
        Guid operationId,
        FolderPath destination,
        CancellationToken cancellationToken)
    {
        CallCount++;
        Subject = subject;
        RestoreDestination = destination.Value;
        RestoreOperationId = operationId;
        return Task.FromResult(TrashRestoreOutcome);
    }

    public Task<TrashEmptyOutcome> EmptyTrashAsync(
        string vaultId,
        string subject,
        CancellationToken cancellationToken)
    {
        CallCount++;
        Subject = subject;
        return Task.FromResult(EmptyTrashOutcome);
    }

    public void SetEntries(string parentPath, IReadOnlyList<VaultEntry> value) => entries[parentPath] = value;

    public void Reset()
    {
        entries.Clear();
        CallCount = 0;
        ParentPath = null;
        CreationResult = FolderCreationResult.Created;
        DeletionResult = FolderDeletionResult.NotFound;
        MoveResult = FolderMoveResult.Moved;
        TrashMoveOutcome = new TrashMoveOutcome(TrashMoveResult.NotFound);
        TrashRestoreOutcome = new TrashRestoreOutcome(TrashRestoreResult.NotFound);
        EmptyTrashOutcome = new TrashEmptyOutcome(0, 0);
        TrashEntries = [];
        Subject = null;
        ExpectedETag = null;
        RestoreDestination = null;
        RestoreOperationId = Guid.Empty;
    }
}

public sealed class FakeTrashRetentionSettingsStore : ITrashRetentionSettingsStore
{
    public string? Subject { get; private set; }

    public int? LastValue { get; private set; }

    public Task<int?> GetRetentionDaysAsync(string subject, CancellationToken cancellationToken)
    {
        Subject = subject;
        return Task.FromResult(LastValue);
    }

    public Task SetRetentionDaysAsync(string subject, int? retentionDays, CancellationToken cancellationToken)
    {
        Subject = subject;
        LastValue = retentionDays;
        return Task.CompletedTask;
    }

    public void Reset()
    {
        Subject = null;
        LastValue = null;
    }
}

public sealed class FakeTrashAuditStore : ITrashAuditStore
{
    private readonly List<TrashAuditEvent> events = [];

    public IReadOnlyList<TrashAuditEvent> Events => events;

    public Task RecordAsync(TrashAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        events.Add(auditEvent);
        return Task.CompletedTask;
    }

    public void Reset() => events.Clear();
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

    public Task<NoteMoveResult> MoveAsync(string vaultId, NotePath source, NotePath destination, string expectedETag, CancellationToken cancellationToken)
    {
        if (!notes.TryGetValue(source.Value, out var note)) return Task.FromResult(NoteMoveResult.NotFound);
        if (note.ETag != expectedETag) return Task.FromResult(NoteMoveResult.VersionConflict);
        if (notes.ContainsKey(destination.Value)) return Task.FromResult(NoteMoveResult.DestinationExists);
        notes.Remove(source.Value);
        notes[destination.Value] = note with { Path = destination };
        return Task.FromResult(NoteMoveResult.Moved);
    }

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
