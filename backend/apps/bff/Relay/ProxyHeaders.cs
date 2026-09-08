using System.Net.Http.Headers;

namespace Altivy.Bff.Relay;

/// <summary>
/// Centraliza a lista de headers hop-by-hop e a cópia de headers entre requisições
/// e respostas encaminhadas pelos proxies do BFF.
/// </summary>
internal static class ProxyHeaders
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Connection",
        "Content-Length",
        "Host",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade"
    };

    /// <summary>
    /// Indica se um header não deve atravessar o proxy BFF.
    /// </summary>
    /// <param name="headerName">Nome do header HTTP.</param>
    /// <returns><see langword="true"/> para headers controlados pelo transporte ou pelo BFF.</returns>
    public static bool IsHopByHop(string headerName) => HopByHopHeaders.Contains(headerName);

    /// <summary>
    /// Copia headers da requisição recebida para a requisição downstream, excluindo hop-by-hop.
    /// </summary>
    /// <param name="source">Headers da requisição recebida pelo BFF.</param>
    /// <param name="destination">Requisição downstream que receberá os headers.</param>
    public static void CopyRequestHeaders(IHeaderDictionary source, HttpRequestMessage destination)
    {
        foreach (var header in source)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            destination.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    /// <summary>
    /// Copia headers de resposta e de conteúdo downstream para a resposta do BFF, excluindo hop-by-hop.
    /// </summary>
    /// <param name="responseHeaders">Headers de resposta retornados pelo downstream.</param>
    /// <param name="contentHeaders">Headers de conteúdo retornados pelo downstream.</param>
    /// <param name="destination">Headers da resposta que o BFF enviará ao browser.</param>
    public static void CopyResponseHeaders(
        HttpResponseHeaders responseHeaders,
        HttpContentHeaders contentHeaders,
        IHeaderDictionary destination)
    {
        foreach (var header in responseHeaders)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                destination[header.Key] = header.Value.ToArray();
            }
        }

        foreach (var header in contentHeaders)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                destination[header.Key] = header.Value.ToArray();
            }
        }
    }
}
