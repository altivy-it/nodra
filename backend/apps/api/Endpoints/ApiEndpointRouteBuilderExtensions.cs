using Altivy.Notes.Api.Authorization;
using Altivy.Notes.Api.Notes;
using Altivy.Notes.Api.Vaults;
using Altivy.Backend.Application.Notes;
using Altivy.Backend.Application.Vaults;
using Altivy.Notes.Api.Realtime;

namespace Altivy.Notes.Api.Endpoints;

/// <summary>
/// Registra os endpoints públicos da API Altivy Notes.
/// </summary>
public static class ApiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Mapeia health, sessão e recurso de vault com autorização contextual.
    /// </summary>
    /// <param name="app">Aplicação configurada.</param>
    /// <returns>A aplicação para composição adicional.</returns>
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        _ = app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }))
            .WithSummary("Verifica se a API está em execução.")
            .WithDescription("Retorna liveness sem exigir autenticação ou consultar recursos externos.");

        _ = app.MapGet("/api/v1/session", (HttpContext context) => Results.Ok(new
        {
            subject = context.User.FindFirst("sub")?.Value,
            authenticated = context.User.Identity?.IsAuthenticated ?? false
        }))
        .RequireAuthorization()
        .WithSummary("Retorna a identidade técnica da sessão.")
        .WithDescription("Exige um access token válido emitido pelo realm Altivy.");

        _ = app.MapGet("/api/v1/vaults", ListVaultsAsync)
            .RequireAuthorization()
            .WithSummary("Lista os vaults autorizados.")
            .WithDescription(
                "Retorna somente vaults com decisão positiva para vault:read e falha integralmente quando a autorização fica indisponível.");

        _ = app.MapGet("/api/v1/vaults/{vaultId}", GetVaultAsync)
            .RequireAuthorization()
            .WithSummary("Obtém um vault autorizado.")
            .WithDescription("Consulta o Keycloak Authorization Services antes de revelar os metadados do vault.");

        var notes = app.MapGroup("/api/v1/vaults/{vaultId}/notes").RequireAuthorization();
        _ = notes.MapGet("", ListNotesAsync).WithSummary("Lista as notas autorizadas do vault.");
        _ = notes.MapGet("{**path}", GetNoteAsync).WithSummary("Obtém uma nota autorizada pelo caminho relativo.");
        _ = notes.MapPost("{**path}", CreateNoteAsync).WithSummary("Cria uma nota Markdown no vault.");
        _ = notes.MapPut("{**path}", UpdateNoteAsync).WithSummary("Atualiza uma nota mediante ETag atual.");
        _ = notes.MapDelete("{**path}", DeleteNoteAsync).WithSummary("Exclui uma nota mediante ETag atual.");

        _ = app.MapHub<VaultHub>("/hubs/vaults")
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListVaultsAsync(
        string? cursor,
        int? limit,
        HttpContext context,
        IAuthorizationDecisionService authorization,
        ListVaultCandidatesQueryHandler vaultQuery,
        CancellationToken cancellationToken)
    {
        var accessToken = GetBearerToken(context);
        if (accessToken is null)
        {
            return Results.Unauthorized();
        }

        var requestedLimit = limit ?? 20;
        if (requestedLimit is < 1 or > 100)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Limite de vaults inválido.");
        }

        try
        {
            var authorizedVaults = new List<VaultSummaryResponse>(requestedLimit);
            var nextCursor = cursor;

            while (authorizedVaults.Count < requestedLimit)
            {
                var remaining = requestedLimit - authorizedVaults.Count;
                var candidateLimit = Math.Min(4, remaining);
                var page = await vaultQuery.HandleAsync(
                    new ListVaultCandidatesQuery(nextCursor, candidateLimit),
                    cancellationToken);

                if (page.Items.Count == 0)
                {
                    nextCursor = null;
                    break;
                }

                var decisions = await Task.WhenAll(page.Items.Select(candidate =>
                    authorization.EvaluateAsync(
                        accessToken,
                        VaultAuthorization.ResourceFor(candidate.Id),
                        VaultAuthorization.ReadScope,
                        cancellationToken)));

                if (decisions.Any(decision => !decision.Available))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "Serviço de autorização indisponível.");
                }

                for (var index = 0; index < page.Items.Count; index++)
                {
                    if (decisions[index].Allowed)
                    {
                        var candidate = page.Items[index];
                        authorizedVaults.Add(new VaultSummaryResponse(candidate.Id, candidate.Name));
                    }
                }

                nextCursor = page.NextCursor;
                if (nextCursor is null)
                {
                    break;
                }
            }

            return Results.Ok(new VaultPageResponse(authorizedVaults, nextCursor));
        }
        catch (InvalidVaultCursorException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Cursor de vault inválido.");
        }
    }

    private static async Task<IResult> GetVaultAsync(
        string vaultId,
        HttpContext context,
        IAuthorizationDecisionService authorization,
        GetVaultQueryHandler vaultQuery,
        CancellationToken cancellationToken)
    {
        if (!VaultId.TryParse(vaultId, out var parsedVaultId))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Identificador de vault inválido.");
        }

        var accessToken = GetBearerToken(context);
        if (accessToken is null)
        {
            return Results.Unauthorized();
        }

        var decision = await authorization.EvaluateAsync(
            accessToken,
            VaultAuthorization.ResourceFor(parsedVaultId.Value),
            VaultAuthorization.ReadScope,
            cancellationToken);

        if (!decision.Available)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Serviço de autorização indisponível.");
        }

        if (!decision.Allowed)
        {
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Acesso negado.");
        }

        var vault = await vaultQuery.HandleAsync(parsedVaultId.Value, cancellationToken);
        if (vault is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new VaultAccessResponse(vault.Id, VaultAuthorization.ReadScope));
    }

    private static string? GetBearerToken(HttpContext context)
    {
        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var accessToken = authorizationHeader["Bearer ".Length..].Trim();
        return accessToken.Length == 0 ? null : accessToken;
    }

    private static async Task<IResult> ListNotesAsync(
        string vaultId, HttpContext context, IAuthorizationDecisionService authorization, ListNotesQueryHandler query,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.ReadScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        var notes = await query.HandleAsync(vaultId, cancellationToken);
        return Results.Ok(notes.Select(note => new { path = note.Path.Value, eTag = note.ETag }));
    }

    private static async Task<IResult> GetNoteAsync(
        string vaultId, string path, HttpContext context, IAuthorizationDecisionService authorization, GetNoteQueryHandler query,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.ReadScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        try
        {
            var note = await query.HandleAsync(vaultId, path, cancellationToken);
            if (note is null) return Results.NotFound();
            context.Response.Headers.ETag = $"\"{note.ETag}\"";
            return Results.Ok(new { path = note.Path.Value, content = note.Content });
        }
        catch (ArgumentException) { return Results.Problem(statusCode: 400, title: "Caminho da nota inválido."); }
    }

    private static async Task<IResult> CreateNoteAsync(
        string vaultId, string path, NoteRequest request, HttpContext context, IAuthorizationDecisionService authorization,
        CreateNoteCommandHandler command, CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.WriteScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        try
        {
            return await command.HandleAsync(new CreateNoteCommand(vaultId, path, request.Content), cancellationToken)
                ? Results.Created($"/api/v1/vaults/{vaultId}/notes/{path}", new { path })
                : Results.Conflict();
        }
        catch (ArgumentException) { return Results.Problem(statusCode: 400, title: "Dados da nota inválidos."); }
    }

    private static async Task<IResult> UpdateNoteAsync(
        string vaultId, string path, NoteRequest request, HttpContext context, IAuthorizationDecisionService authorization,
        UpdateNoteCommandHandler command, CancellationToken cancellationToken) =>
        await WriteNoteAsync(vaultId, request.Content, context, authorization,
            (content, tag) => command.HandleAsync(vaultId, path, content, tag, cancellationToken), cancellationToken);

    private static async Task<IResult> DeleteNoteAsync(
        string vaultId, string path, HttpContext context, IAuthorizationDecisionService authorization,
        DeleteNoteCommandHandler command, CancellationToken cancellationToken) =>
        await WriteNoteAsync(vaultId, null, context, authorization,
            (_, tag) => command.HandleAsync(vaultId, path, tag, cancellationToken), cancellationToken);

    private static async Task<IResult> WriteNoteAsync(
        string vaultId, string? content, HttpContext context, IAuthorizationDecisionService authorization,
        Func<string, string, Task<NoteWriteResult>> write, CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, content is null ? VaultAuthorization.DeleteScope : VaultAuthorization.WriteScope,
            context, authorization, cancellationToken);
        if (access is not null) return access;
        var etag = context.Request.Headers.IfMatch.ToString().Trim('"');
        if (string.IsNullOrWhiteSpace(etag)) return Results.Problem(statusCode: 428, title: "If-Match é obrigatório.");
        try
        {
            return await write(content ?? string.Empty, etag) switch
            {
                NoteWriteResult.Updated => Results.NoContent(),
                NoteWriteResult.NotFound => Results.NotFound(),
                _ => Results.Conflict()
            };
        }
        catch (ArgumentException) { return Results.Problem(statusCode: 400, title: "Dados da nota inválidos."); }
    }

    private static async Task<IResult?> AuthorizeAsync(
        string vaultId, string scope, HttpContext context, IAuthorizationDecisionService authorization,
        CancellationToken cancellationToken)
    {
        if (!VaultId.TryParse(vaultId, out var parsedVaultId)) return Results.Problem(statusCode: 400, title: "Identificador de vault inválido.");
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Results.Unauthorized();
        var decision = await authorization.EvaluateAsync(header["Bearer ".Length..].Trim(), VaultAuthorization.ResourceFor(parsedVaultId.Value), scope, cancellationToken);
        if (!decision.Available) return Results.Problem(statusCode: 503, title: "Serviço de autorização indisponível.");
        return decision.Allowed ? null : Results.Problem(statusCode: 403, title: "Acesso negado.");
    }
}
