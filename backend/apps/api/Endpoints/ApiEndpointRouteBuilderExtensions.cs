using Nodra.Backend.Application.Notes;
using Nodra.Backend.Application.Vaults;
using Nodra.Notes.Api.Authorization;
using Nodra.Notes.Api.Notes;
using Nodra.Notes.Api.Realtime;
using Nodra.Notes.Api.Vaults;

namespace Nodra.Notes.Api.Endpoints;

/// <summary>
/// Registra os endpoints públicos da API Nodra.
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

        MapTrashSettingsEndpoints(app);

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
        _ = notes.MapDelete("{**path}", DeleteNoteAsync)
            .WithSummary("Move uma nota para a lixeira mediante ETag atual.")
            .WithDescription("Exige vault:delete e If-Match. Move a nota para a lixeira sem remover seu conteúdo permanentemente.");
        _ = notes.MapPatch("{**path}", MoveNoteAsync)
            .WithSummary("Move ou renomeia uma nota mediante ETag atual.")
            .WithDescription(
                "Exige vault:write e If-Match. Altera o caminho da nota sem sobrescrever o destino nem criar pastas; o conteúdo e o ETag são preservados.");

        _ = app.MapGet("/api/v1/vaults/{vaultId}/entries", ListFolderChildrenAsync)
            .RequireAuthorization()
            .WithSummary("Lista as entradas imediatas de uma pasta autorizada.")
            .WithDescription(
                "Exige vault:read e retorna somente pastas e notas Markdown diretamente contidas na pasta informada; parentPath ausente representa a raiz.");

        _ = app.MapPut("/api/v1/vaults/{vaultId}/folders/{**path}", EnsureFolderAsync)
            .RequireAuthorization()
            .WithSummary("Garante a existência de uma pasta no vault.")
            .WithDescription(
                "Exige vault:write. O PUT é idempotente: cria a pasta relativa quando ausente, retorna sem alteração quando ela já existe e não cria pastas pai implicitamente.");

        _ = app.MapDelete("/api/v1/vaults/{vaultId}/folders/{**path}", DeleteFolderAsync)
            .RequireAuthorization()
            .WithSummary("Move uma pasta para a lixeira.")
            .WithDescription(
                "Exige vault:delete. Move a pasta e todo seu conteúdo para a lixeira do sujeito autenticado; não exclui conteúdo permanentemente.");

        MapTrashEndpoints(app);

        _ = app.MapPatch("/api/v1/vaults/{vaultId}/folders/{**path}", MoveFolderAsync)
            .RequireAuthorization()
            .WithSummary("Move ou renomeia uma pasta do vault.")
            .WithDescription(
                "Exige vault:write. Move a pasta com todo o conteúdo sem sobrescrever o destino nem criar pastas pai; o destino não pode estar dentro da origem.");

        _ = app.MapHub<VaultHub>("/hubs/vaults")
            .RequireAuthorization();

        return app;
    }

    private static void MapTrashEndpoints(WebApplication app)
    {
        _ = app.MapGet("/api/v1/vaults/{vaultId}/trash", ListTrashAsync)
            .RequireAuthorization()
            .WithSummary("Lista uma página da lixeira do sujeito autenticado.")
            .WithDescription("Exige vault:read e retorna somente metadados mínimos das operações de lixeira pertencentes ao sub autenticado.");

        _ = app.MapPatch("/api/v1/vaults/{vaultId}/trash/{operationId:guid}", RestoreTrashEntryAsync)
            .RequireAuthorization()
            .WithSummary("Restaura uma operação da lixeira para um destino informado.")
            .WithDescription("Exige vault:write. O destino é explícito, relativo e nunca sobrescreve uma entrada existente.");

        _ = app.MapDelete("/api/v1/vaults/{vaultId}/trash", TrashPurgeEndpoint.HandleAsync)
            .RequireAuthorization()
            .WithSummary("Esvazia a lixeira do sujeito após reautenticação recente.")
            .WithDescription("Exige vault:delete, confirmação no cliente e auth_time recente propagado pelo BFF. A operação é auditada antes e depois do filesystem.");
    }

    private static void MapTrashSettingsEndpoints(WebApplication app)
    {
        _ = app.MapGet("/api/v1/me/trash-settings", TrashSettingsEndpoint.GetAsync)
            .RequireAuthorization()
            .WithSummary("Obtém o parâmetro de retenção da lixeira do sujeito.")
            .WithDescription("Retorna somente o parâmetro não autorizativo do sub autenticado; ausência de configuração retorna null.");

        _ = app.MapPut("/api/v1/me/trash-settings", TrashSettingsEndpoint.SetAsync)
            .RequireAuthorization()
            .WithSummary("Atualiza o parâmetro de retenção da lixeira do sujeito.")
            .WithDescription("Aceita null para desabilitar a remoção automática. A alteração não habilita purge sem step-up e auditoria.");
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

    private static async Task<IResult> ListFolderChildrenAsync(
        string vaultId,
        string? parentPath,
        HttpContext context,
        IAuthorizationDecisionService authorization,
        ListFolderChildrenQueryHandler query,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.ReadScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        try
        {
            var entries = await query.HandleAsync(vaultId, parentPath, cancellationToken);
            return Results.Ok(entries.Select(entry => new
            {
                path = entry.Path,
                name = entry.Name,
                kind = entry.IsFolder ? "folder" : "note",
                eTag = entry.ETag,
                isUnclassified = entry.IsUnclassified,
                hasMinimumMetadata = entry.HasMinimumMetadata,
                hasUnresolvedLinks = entry.HasUnresolvedLinks
            }));
        }
        catch (ArgumentException)
        {
            return Results.Problem(statusCode: 400, title: "Caminho da pasta inválido.");
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Problem(statusCode: 403, title: "Acesso ao conteúdo do vault negado.");
        }
    }

    private static async Task<IResult> EnsureFolderAsync(
        string vaultId,
        string path,
        HttpContext context,
        IAuthorizationDecisionService authorization,
        EnsureFolderCommandHandler command,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.WriteScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        try
        {
            var result = await command.HandleAsync(
                vaultId,
                Uri.UnescapeDataString(path),
                cancellationToken);
            return result switch
            {
                FolderCreationResult.Created => Results.Created(FolderLocation(vaultId, path), null),
                FolderCreationResult.AlreadyExists => Results.NoContent(),
                _ => Results.Conflict()
            };
        }
        catch (ArgumentException)
        {
            return Results.Problem(statusCode: 400, title: "Caminho da pasta inválido.");
        }
        catch (DirectoryNotFoundException)
        {
            return Results.Problem(statusCode: 404, title: "A pasta pai não foi encontrada.");
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Problem(statusCode: 403, title: "Acesso ao conteúdo do vault negado.");
        }
    }

    private static string FolderLocation(string vaultId, string path) =>
        $"/api/v1/vaults/{Uri.EscapeDataString(vaultId)}/folders/" +
        string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    private static async Task<IResult> MoveFolderAsync(
        string vaultId,
        string path,
        FolderMoveRequest request,
        HttpContext context,
        IAuthorizationDecisionService authorization,
        MoveFolderCommandHandler command,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.WriteScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        try
        {
            var result = await command.HandleAsync(vaultId, Uri.UnescapeDataString(path), request.Path, cancellationToken);
            return result switch
            {
                FolderMoveResult.Moved => Results.Ok(new { path = request.Path }),
                FolderMoveResult.NotFound => Results.NotFound(),
                FolderMoveResult.DestinationExists => Results.Problem(
                    statusCode: 409, title: "Já existe um item no caminho de destino.", extensions: ProblemCode("folder-destination-exists")),
                _ => Results.Problem(
                    statusCode: 409, title: "A pasta de destino não existe.", extensions: ProblemCode("folder-destination-folder-missing"))
            };
        }
        catch (ArgumentException)
        {
            return Results.Problem(statusCode: 400, title: "Caminho da pasta inválido.");
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Problem(statusCode: 403, title: "Acesso ao conteúdo do vault negado.");
        }
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
        MoveEntryToTrashCommandHandler command, CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.DeleteScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        var etag = context.Request.Headers.IfMatch.ToString().Trim('"');
        if (string.IsNullOrWhiteSpace(etag)) return Results.Problem(statusCode: 428, title: "If-Match é obrigatório.");
        var subject = GetSubject(context);
        if (subject is null) return Results.Unauthorized();
        try
        {
            var result = await command.HandleAsync(vaultId, subject, Uri.UnescapeDataString(path), etag, cancellationToken);
            return TrashMoveResultResponse(result);
        }
        catch (ArgumentException) { return Results.Problem(statusCode: 400, title: "Caminho da nota inválido."); }
        catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Acesso ao conteúdo do vault negado."); }
    }

    private static async Task<IResult> DeleteFolderAsync(
        string vaultId,
        string path,
        HttpContext context,
        IAuthorizationDecisionService authorization,
        MoveEntryToTrashCommandHandler command,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.DeleteScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        var subject = GetSubject(context);
        if (subject is null) return Results.Unauthorized();
        try
        {
            var result = await command.HandleAsync(vaultId, subject, Uri.UnescapeDataString(path), null, cancellationToken);
            return TrashMoveResultResponse(result);
        }
        catch (ArgumentException) { return Results.Problem(statusCode: 400, title: "Caminho da pasta inválido."); }
        catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Acesso ao conteúdo do vault negado."); }
    }

    private static async Task<IResult> ListTrashAsync(
        string vaultId,
        string? cursor,
        int? limit,
        HttpContext context,
        IAuthorizationDecisionService authorization,
        ListTrashQueryHandler query,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.ReadScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        var subject = GetSubject(context);
        if (subject is null) return Results.Unauthorized();
        try
        {
            var page = await query.HandleAsync(vaultId, subject, cursor, limit ?? 20, cancellationToken);
            return Results.Ok(new
            {
                items = page.Items.Select(entry => new
                {
                    id = entry.Id,
                    path = entry.OriginalPath,
                    name = entry.Name,
                    kind = entry.IsFolder ? "folder" : "note",
                    deletedAtUtc = entry.DeletedAtUtc,
                    retentionDays = entry.RetentionDays,
                    canRestoreToOriginal = entry.CanRestoreToOriginal
                }),
                nextCursor = page.NextCursor
            });
        }
        catch (ArgumentException) { return Results.Problem(statusCode: 400, title: "Cursor ou limite da lixeira inválido."); }
        catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Acesso à lixeira negado."); }
    }

    private static async Task<IResult> RestoreTrashEntryAsync(
        string vaultId,
        Guid operationId,
        TrashRestoreRequest request,
        HttpContext context,
        IAuthorizationDecisionService authorization,
        RestoreTrashEntryCommandHandler command,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.WriteScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        var subject = GetSubject(context);
        if (subject is null) return Results.Unauthorized();
        try
        {
            var outcome = await command.HandleAsync(vaultId, subject, operationId, request.Path, cancellationToken);
            return outcome.Result switch
            {
                TrashRestoreResult.Restored => Results.NoContent(),
                TrashRestoreResult.NotFound => Results.NotFound(),
                TrashRestoreResult.DestinationFolderNotFound => Results.Problem(
                    statusCode: 409, title: "A pasta de destino não existe.", extensions: ProblemCode("trash-destination-folder-missing")),
                _ => Results.Problem(
                    statusCode: 409, title: "O destino informado já está ocupado.", extensions: ProblemCode("trash-destination-exists"))
            };
        }
        catch (ArgumentException) { return Results.Problem(statusCode: 400, title: "Caminho de restauração inválido."); }
        catch (UnauthorizedAccessException) { return Results.Problem(statusCode: 403, title: "Acesso à lixeira negado."); }
    }

    private static IResult TrashMoveResultResponse(TrashMoveOutcome result) => result.Result switch
    {
        TrashMoveResult.Moved => Results.NoContent(),
        TrashMoveResult.NotFound => Results.NotFound(),
        TrashMoveResult.VersionConflict => Results.Problem(
            statusCode: 409, title: "A nota foi alterada por outra sessão.", extensions: ProblemCode("note-version-conflict")),
        _ => Results.Problem(statusCode: 409, title: "Apenas notas Markdown e pastas podem ir para a lixeira.",
            extensions: ProblemCode("trash-entry-unsupported"))
    };

    private static string? GetSubject(HttpContext context)
    {
        var subject = context.User.FindFirst("sub")?.Value;
        return string.IsNullOrWhiteSpace(subject) ? null : subject;
    }

    private static async Task<IResult> MoveNoteAsync(
        string vaultId, string path, NoteMoveRequest request, HttpContext context, IAuthorizationDecisionService authorization,
        MoveNoteCommandHandler command, CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(vaultId, VaultAuthorization.WriteScope, context, authorization, cancellationToken);
        if (access is not null) return access;
        var etag = context.Request.Headers.IfMatch.ToString().Trim('"');
        if (string.IsNullOrWhiteSpace(etag)) return Results.Problem(statusCode: 428, title: "If-Match é obrigatório.");
        try
        {
            var source = Uri.UnescapeDataString(path);
            var result = await command.HandleAsync(vaultId, source, request.Path, etag, cancellationToken);
            return result switch
            {
                NoteMoveResult.Moved => Results.Ok(new { path = request.Path }),
                NoteMoveResult.NotFound => Results.NotFound(),
                NoteMoveResult.VersionConflict => Results.Problem(
                    statusCode: 409, title: "A nota foi alterada por outra sessão.", extensions: ProblemCode("note-version-conflict")),
                NoteMoveResult.DestinationExists => Results.Problem(
                    statusCode: 409, title: "Já existe um item no caminho de destino.", extensions: ProblemCode("note-destination-exists")),
                _ => Results.Problem(
                    statusCode: 409, title: "A pasta de destino não existe.", extensions: ProblemCode("note-destination-folder-missing"))
            };
        }
        catch (ArgumentException) { return Results.Problem(statusCode: 400, title: "Caminho da nota inválido."); }
    }

    private static Dictionary<string, object?> ProblemCode(string code) => new() { ["code"] = code };

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
