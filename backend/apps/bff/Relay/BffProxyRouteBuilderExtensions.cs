using System.Net.Http.Headers;
using Nodra.Bff.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Nodra.Bff.Relay;

/// <summary>
/// Registra o encaminhamento server-side entre o BFF e a API Altivy.
/// </summary>
public static class BffProxyRouteBuilderExtensions
{
    /// <summary>
    /// Indica se um header não deve atravessar o proxy BFF.
    /// </summary>
    /// <param name="headerName">Nome do header HTTP.</param>
    /// <returns><see langword="true"/> para headers controlados pelo transporte ou pelo BFF.</returns>
    public static bool IsHopByHopHeader(string headerName) => ProxyHeaders.IsHopByHop(headerName);

    /// <summary>
    /// Mapeia o proxy autenticado para recursos da API sob `/api/v1`.
    /// </summary>
    /// <param name="endpoints">Construtor de rotas do BFF.</param>
    /// <returns>O mesmo construtor para composição adicional.</returns>
    public static IEndpointRouteBuilder MapBffApiProxy(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMethods(
            "/api/v1/{**path}",
            ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "QUERY"],
            ForwardAsync)
        .RequireAuthorization();

        return endpoints;
    }

    private static async Task ForwardAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        string? path,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("NodraApi");
        var downstreamUri = BuildDownstreamUri(client.BaseAddress, path, context.Request.QueryString);
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), downstreamUri);

        if (context.Request.ContentLength is > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            request.Content = new StreamContent(context.Request.Body);
            if (context.Request.ContentType is not null)
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
            }
        }

        ProxyHeaders.CopyRequestHeaders(context.Request.Headers, request);
        request.Headers.Remove("X-Altivy-Reauth-At");
        if (HttpMethods.IsDelete(context.Request.Method) &&
            path?.StartsWith("vaults/", StringComparison.OrdinalIgnoreCase) == true &&
            path.Contains("/trash", StringComparison.OrdinalIgnoreCase))
        {
            var authentication = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (authentication.Succeeded && authentication.Ticket is not null &&
                ReauthenticationEvidence.TryRead(authentication.Ticket.Properties, context.User, out var evidence) &&
                evidence is not null &&
                evidence.IsFresh(DateTimeOffset.UtcNow, ReauthenticationEvidence.FreshnessWindow))
            {
                request.Headers.TryAddWithoutValidation("X-Altivy-Reauth-At", evidence.AuthenticatedAtUtc.ToString("O"));
            }
        }

        try
        {
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            context.Response.StatusCode = (int)response.StatusCode;
            ProxyHeaders.CopyResponseHeaders(response.Headers, response.Content.Headers, context.Response.Headers);
            await response.Content.CopyToAsync(context.Response.Body, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        catch (HttpRequestException)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
        }
    }

    /// <summary>
    /// Cria a URL downstream mantendo o caminho da API e a query original.
    /// </summary>
    /// <param name="baseAddress">URL base configurada da API.</param>
    /// <param name="path">Caminho relativo recebido pelo BFF.</param>
    /// <param name="queryString">Query string original da requisição.</param>
    /// <returns>URL absoluta para a API downstream.</returns>
    /// <exception cref="InvalidOperationException">Quando a URL base não foi configurada.</exception>
    public static Uri BuildDownstreamUri(Uri? baseAddress, string? path, QueryString queryString)
    {
        if (baseAddress is null)
        {
            throw new InvalidOperationException("O cliente downstream não possui BaseAddress.");
        }

        var relativePath = string.IsNullOrWhiteSpace(path) ? string.Empty : path;
        return new Uri(baseAddress, $"api/v1/{relativePath}{queryString}");
    }
}
