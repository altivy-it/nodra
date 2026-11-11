using Nodra.Bff.Authentication;
using Nodra.Bff.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Nodra.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class KeycloakOptionsValidatorTests
{
    [Fact]
    public void ValidateWithDedicatedLocalRealmInDevelopmentSucceeds()
    {
        var validator = CreateValidator("Development");

        var result = validator.Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithMasterRealmFails()
    {
        var validator = CreateValidator("Development");
        var options = ValidOptions(authority: "http://localhost:8080/realms/master");

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidateWithMasterRealmAndTrailingSlashFails()
    {
        var validator = CreateValidator("Development");
        var options = ValidOptions(authority: "http://localhost:8080/realms/master/");

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidateWithHttpOutsideDevelopmentFails()
    {
        var validator = CreateValidator("Production");

        var result = validator.Validate(null, ValidOptions());

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidateWithExternalCallbackFails()
    {
        var validator = CreateValidator("Development");
        var options = ValidOptions(callbackPath: "//attacker.example/callback");

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidateWithDevelopmentOptInAllowsDockerHostHttp()
    {
        var validator = CreateValidator("Development", allowInsecureHttp: true);
        var options = ValidOptions(authority: "http://host.docker.internal:8080/realms/altivy");

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithProductionOptInStillRejectsDockerHostHttp()
    {
        var validator = CreateValidator("Production", allowInsecureHttp: true);
        var options = ValidOptions(authority: "http://host.docker.internal:8080/realms/altivy");

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    private static KeycloakOptionsValidator CreateValidator(string environmentName, bool allowInsecureHttp = false) =>
        new(
            new TestHostEnvironment(environmentName),
            Options.Create(new BffRuntimeOptions { AllowInsecureHttpForDevelopment = allowInsecureHttp }));

    private static KeycloakOptions ValidOptions(
        string authority = "http://localhost:8080/realms/altivy",
        string callbackPath = "/signin-oidc") => new()
        {
            Authority = authority,
            ClientId = "altivy-bff",
            ClientSecret = "synthetic-test-secret",
            CallbackPath = callbackPath
        };

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Nodra.Bff.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
