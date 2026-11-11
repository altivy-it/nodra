using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Nodra.Bff.Authentication;

/// <summary>
/// Configura o processamento seguro de headers encaminhados por proxies confiáveis.
/// </summary>
public sealed class ConfigureForwardedHeadersOptions(IOptions<BffSecurityOptions> securityOptions)
    : IConfigureOptions<ForwardedHeadersOptions>
{
    /// <summary>
    /// Habilita os headers encaminhados e registra os proxies autorizados.
    /// </summary>
    /// <param name="options">Opções de headers encaminhados que serão configuradas.</param>
    public void Configure(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedHost
            | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = null;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var proxy in securityOptions.Value.TrustedProxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        foreach (var network in securityOptions.Value.TrustedNetworks)
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }
    }
}
