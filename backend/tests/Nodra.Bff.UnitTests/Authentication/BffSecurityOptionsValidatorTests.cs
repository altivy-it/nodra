using Microsoft.Extensions.FileProviders;
using Nodra.Bff.Authentication;

namespace Nodra.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class BffSecurityOptionsValidatorTests
{
    [Fact]
    public void ValidateWithExplicitProxySucceeds()
    {
        var validator = new BffSecurityOptionsValidator(new TestHostEnvironment());

        var result = validator.Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithoutTrustedProxyFails()
    {
        var validator = new BffSecurityOptionsValidator(new TestHostEnvironment());
        var options = new BffSecurityOptions
        {
            DataProtectionKeysPath = "synthetic-test-path",
            AllowedOrigins = ["https://altivy.example"]
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidateWithInvalidTrustedProxyFails()
    {
        var validator = new BffSecurityOptionsValidator(new TestHostEnvironment());
        var options = new BffSecurityOptions
        {
            DataProtectionKeysPath = "synthetic-test-path",
            AllowedOrigins = ["https://altivy.example"],
            TrustedProxies = ["proxy.example"]
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidateInProductionWithoutCertificateFails()
    {
        var validator = new BffSecurityOptionsValidator(new TestHostEnvironment
        {
            EnvironmentName = "Production"
        });

        var result = validator.Validate(null, ValidOptions());

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidateInProductionWithCertificateConfigurationSucceeds()
    {
        var validator = new BffSecurityOptionsValidator(new TestHostEnvironment
        {
            EnvironmentName = "Production"
        });
        var options = new BffSecurityOptions
        {
            DataProtectionKeysPath = "synthetic-test-path",
            DataProtectionCertificatePath = "synthetic-test-certificate.pfx",
            DataProtectionCertificatePassword = "synthetic-test-password",
            AllowedOrigins = ["https://altivy.example"],
            TrustedProxies = ["192.0.2.10"]
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithInvalidTrustedNetworksFails()
    {
        var validator = new BffSecurityOptionsValidator(new TestHostEnvironment());
        var options = new BffSecurityOptions
        {
            DataProtectionKeysPath = "synthetic-test-path",
            AllowedOrigins = ["https://altivy.example"],
            TrustedNetworks = ["invalid-network"]
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidateWithValidTrustedNetworksSucceeds()
    {
        var validator = new BffSecurityOptionsValidator(new TestHostEnvironment());
        var options = new BffSecurityOptions
        {
            DataProtectionKeysPath = "synthetic-test-path",
            AllowedOrigins = ["https://altivy.example"],
            TrustedNetworks = ["172.30.0.0/24"]
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    private static BffSecurityOptions ValidOptions() => new()
    {
        DataProtectionKeysPath = "synthetic-test-path",
        AllowedOrigins = ["https://altivy.example"],
        TrustedProxies = ["192.0.2.10"]
    };

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Nodra.Bff.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
