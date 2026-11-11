using Nodra.Backend.Application.Notes;
using Nodra.Notes.Api.Notes;

namespace Nodra.Notes.Api.Endpoints;

internal static class TrashSettingsEndpoint
{
    internal static async Task<IResult> GetAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var subject = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
        var store = context.RequestServices.GetService(typeof(ITrashRetentionSettingsStore))
            as ITrashRetentionSettingsStore;
        if (store is null) return PersistenceUnavailable();

        try
        {
            return Results.Ok(new { retentionDays = await store.GetRetentionDaysAsync(subject, cancellationToken) });
        }
        catch (InvalidOperationException)
        {
            return PersistenceUnavailable();
        }
    }

    internal static async Task<IResult> SetAsync(
        TrashSettingsRequest request,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var subject = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
        if (request.RetentionDays is < 1 or > 3650)
        {
            return Results.Problem(statusCode: 400, title: "O prazo de retenção deve estar entre 1 e 3650 dias, ou ser null.");
        }

        var store = context.RequestServices.GetService(typeof(ITrashRetentionSettingsStore))
            as ITrashRetentionSettingsStore;
        if (store is null) return PersistenceUnavailable();

        try
        {
            await store.SetRetentionDaysAsync(subject, request.RetentionDays, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException)
        {
            return PersistenceUnavailable();
        }
    }

    private static IResult PersistenceUnavailable() =>
        Results.Problem(statusCode: 503, title: "Persistência de parâmetros indisponível.");
}
