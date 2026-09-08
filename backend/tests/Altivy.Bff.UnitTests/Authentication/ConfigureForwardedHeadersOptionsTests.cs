using System.Net;
using Altivy.Bff.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Altivy.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class ConfigureForwardedHeadersOptionsTests
{
    [Fact]
    public void ConfigureTrustsOnlyConfiguredProxyAndOneForward()
    {
        var security = Options.Create(new BffSecurityOptions
        {
            DataProtectionKeysPath = "synthetic-test-path",
            AllowedOrigins = ["https://altivy.example"],
            TrustedProxies = ["192.0.2.10"]
        });
        var options = new ForwardedHeadersOptions();

        new ConfigureForwardedHeadersOptions(security).Configure(options);

        Assert.Null(options.ForwardLimit);
        Assert.Equal([IPAddress.Parse("192.0.2.10")], options.KnownProxies);
        Assert.Empty(options.KnownIPNetworks);
        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor));
        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedHost));
        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto));
    }

    [Fact]
    public void ConfigureRegistersTrustedNetworks()
    {
        var security = Options.Create(new BffSecurityOptions
        {
            DataProtectionKeysPath = "synthetic-test-path",
            AllowedOrigins = ["https://altivy.example"],
            TrustedNetworks = ["172.30.0.0/24"]
        });
        var options = new ForwardedHeadersOptions();

        new ConfigureForwardedHeadersOptions(security).Configure(options);

        Assert.Equal([System.Net.IPNetwork.Parse("172.30.0.0/24")], options.KnownIPNetworks);
    }
}
