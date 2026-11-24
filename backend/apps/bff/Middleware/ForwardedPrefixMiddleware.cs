using System.Net;
using Nodra.Bff.Authentication;

namespace Nodra.Bff.Middleware;

/// <summary>
/// Aplica o prefixo público encaminhado somente quando a conexão veio de proxy confiável.
/// </summary>
public sealed class ForwardedPrefixMiddleware(
    RequestDelegate next,
    IOptions<BffSecurityOptions> securityOptions)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var prefix = context.Request.Headers["X-Forwarded-Prefix"].ToString();
        _ = context.Request.Headers.Remove("X-Forwarded-Prefix");

        if (IsTrustedProxy(context.Connection.RemoteIpAddress) &&
            string.Equals(prefix, "/nodra", StringComparison.Ordinal))
        {
            context.Request.PathBase = new PathString(prefix);
        }

        await next(context);
    }

    private bool IsTrustedProxy(IPAddress? remoteAddress)
    {
        if (remoteAddress is null) return false;
        var normalizedRemote = remoteAddress.IsIPv4MappedToIPv6 ? remoteAddress.MapToIPv4() : remoteAddress;
        return securityOptions.Value.TrustedProxies.Any(proxy =>
            IPAddress.TryParse(proxy, out var trustedAddress) && normalizedRemote.Equals(trustedAddress));
    }
}
