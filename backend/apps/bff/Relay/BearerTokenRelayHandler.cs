using System.Net.Http.Headers;

namespace Altivy.Bff.Relay;

/// <summary>
/// Adiciona o token de acesso da sessão ao header de autorização de chamadas downstream.
/// </summary>
public sealed class BearerTokenRelayHandler(IAccessTokenProvider accessTokenProvider) : DelegatingHandler
{
    /// <summary>
    /// Obtém o token da sessão, adiciona o esquema Bearer e envia a requisição.
    /// </summary>
    /// <param name="request">Requisição HTTP downstream.</param>
    /// <param name="cancellationToken">Token para cancelar a chamada.</param>
    /// <returns>Resposta HTTP retornada pelo próximo handler da cadeia.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await accessTokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}