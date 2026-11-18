using System.Net.Http.Headers;
using System.Net.WebSockets;

namespace Nodra.Bff.Relay;

/// <summary>
/// Registra o encaminhamento server-side entre o BFF e os hubs SignalR da API Altivy.
/// </summary>
public static class BffHubsProxyRouteBuilderExtensions
{
    /// <summary>
    /// Mapeia o proxy autenticado para hubs SignalR da API sob `/hubs`, cobrindo negotiate,
    /// long polling, server-sent events e upgrade de WebSocket.
    /// </summary>
    /// <param name="endpoints">Construtor de rotas do BFF.</param>
    /// <returns>O mesmo construtor para composição adicional.</returns>
    public static IEndpointRouteBuilder MapBffHubsProxy(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapMethods(
            "/hubs/{**path}",
            ["GET", "POST"],
            ForwardAsync)
        .RequireAuthorization();

        return endpoints;
    }

    private static async Task ForwardAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        IAccessTokenProvider accessTokenProvider,
        string? path,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("NodraApi");

        if (context.WebSockets.IsWebSocketRequest)
        {
            await ProxyWebSocketAsync(context, client.BaseAddress, accessTokenProvider, path, cancellationToken);
            return;
        }

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
        catch (HttpRequestException)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
        }
    }

    private static async Task ProxyWebSocketAsync(
        HttpContext context,
        Uri? baseAddress,
        IAccessTokenProvider accessTokenProvider,
        string? path,
        CancellationToken cancellationToken)
    {
        var downstreamUri = BuildDownstreamWebSocketUri(baseAddress, path, context.Request.QueryString);
        var accessToken = await accessTokenProvider.GetAccessTokenAsync(cancellationToken);

        using var downstreamSocket = new ClientWebSocket();
        downstreamSocket.Options.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        foreach (var protocol in context.WebSockets.WebSocketRequestedProtocols)
        {
            downstreamSocket.Options.AddSubProtocol(protocol);
        }

        try
        {
            await downstreamSocket.ConnectAsync(downstreamUri, cancellationToken);
        }
        catch (WebSocketException)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            return;
        }

        using var localSocket = await context.WebSockets.AcceptWebSocketAsync(downstreamSocket.SubProtocol);

        var toDownstream = PumpAsync(localSocket, downstreamSocket, cancellationToken);
        var toLocal = PumpAsync(downstreamSocket, localSocket, cancellationToken);
        _ = await Task.WhenAny(toDownstream, toLocal);
    }

    private static async Task PumpAsync(WebSocket source, WebSocket destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        while (source.State == WebSocketState.Open && destination.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await source.ReceiveAsync(buffer, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (WebSocketException)
            {
                return;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                try
                {
                    await destination.CloseOutputAsync(
                        source.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        source.CloseStatusDescription,
                        cancellationToken);
                }
                catch (WebSocketException)
                {
                    // a outra direção do pump já fechou o destino nesta corrida
                }
                catch (InvalidOperationException)
                {
                    // destino em estado que não aceita fechamento nesta corrida entre as duas direções do pump
                }

                return;
            }

            await destination.SendAsync(
                new ArraySegment<byte>(buffer, 0, result.Count),
                result.MessageType,
                result.EndOfMessage,
                cancellationToken);
        }
    }

    /// <summary>
    /// Cria a URL downstream HTTP mantendo o caminho do hub e a query original.
    /// </summary>
    /// <param name="baseAddress">URL base configurada da API.</param>
    /// <param name="path">Caminho relativo recebido pelo BFF.</param>
    /// <param name="queryString">Query string original da requisição.</param>
    /// <returns>URL absoluta para o hub downstream.</returns>
    /// <exception cref="InvalidOperationException">Quando a URL base não foi configurada.</exception>
    public static Uri BuildDownstreamUri(Uri? baseAddress, string? path, QueryString queryString)
    {
        if (baseAddress is null)
        {
            throw new InvalidOperationException("O cliente downstream não possui BaseAddress.");
        }

        var relativePath = string.IsNullOrWhiteSpace(path) ? string.Empty : path;
        return new Uri(baseAddress, $"hubs/{relativePath}{queryString}");
    }

    /// <summary>
    /// Converte a URL downstream HTTP para o esquema WebSocket equivalente.
    /// </summary>
    /// <param name="baseAddress">URL base configurada da API.</param>
    /// <param name="path">Caminho relativo recebido pelo BFF.</param>
    /// <param name="queryString">Query string original da requisição.</param>
    /// <returns>URL <c>ws</c> ou <c>wss</c> para o hub downstream.</returns>
    public static Uri BuildDownstreamWebSocketUri(Uri? baseAddress, string? path, QueryString queryString)
    {
        var httpUri = BuildDownstreamUri(baseAddress, path, queryString);
        return new UriBuilder(httpUri)
        {
            Scheme = httpUri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws"
        }.Uri;
    }
}
