namespace Altivy.Bff.Authentication;

/// <summary>
/// Bloqueia requisições mutáveis cuja origem não esteja autorizada pelo BFF.
/// </summary>
public sealed class OriginValidationMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Valida a origem da requisição e encaminha a chamada quando ela é permitida.
    /// </summary>
    /// <param name="context">Contexto HTTP da requisição atual.</param>
    /// <param name="originValidator">Validador das origens configuradas.</param>
    /// <returns>Uma tarefa que representa o processamento da requisição.</returns>
    public async Task InvokeAsync(HttpContext context, RequestOriginValidator originValidator)
    {
        // upgrade de WebSocket usa método GET, mas exige a mesma validação de origem que mutações
        var requiresOriginCheck = context.WebSockets.IsWebSocketRequest
            || (!HttpMethods.IsGet(context.Request.Method)
                && !HttpMethods.IsHead(context.Request.Method)
                && !HttpMethods.IsOptions(context.Request.Method));

        if (requiresOriginCheck && !originValidator.IsAllowed(context.Request.Headers.Origin))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }
}