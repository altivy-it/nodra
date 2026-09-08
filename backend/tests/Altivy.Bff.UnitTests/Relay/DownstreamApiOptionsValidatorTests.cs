using Altivy.Bff.Configuration;
using Altivy.Bff.Relay;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Altivy.Bff.UnitTests.Relay;

[Trait("Category", "Unit")]
public sealed class DownstreamApiOptionsValidatorTests
{
    [Fact]
    public void ValidateWithHttpsApiSucceeds()
    {
        var validator = CreateValidator("Production");

        var result = validator.Validate(null, new DownstreamApiOptions
        {
            BaseUrl = "https://api.internal.example"
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithRemoteHttpApiFails()
    {
        var validator = CreateValidator("Production");

        var result = validator.Validate(null, new DownstreamApiOptions
        {
            BaseUrl = "http://api.internal.example"
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidateWithLoopbackHttpApiInDevelopmentSucceeds()
    {
        var validator = CreateValidator("Development");

        var result = validator.Validate(null, new DownstreamApiOptions
        {
            BaseUrl = "http://localhost:5080"
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithDevelopmentOptInAllowsDockerServiceHttp()
    {
        var validator = CreateValidator("Development", allowInsecureHttp: true);

        var result = validator.Validate(null, new DownstreamApiOptions
        {
            BaseUrl = "http://api:8080"
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithDevelopmentWithoutOptInRejectsDockerServiceHttp()
    {
        var validator = CreateValidator("Development");

        var result = validator.Validate(null, new DownstreamApiOptions
        {
            BaseUrl = "http://api:8080"
        });

        Assert.True(result.Failed);
    }

    private static DownstreamApiOptionsValidator CreateValidator(
        string environmentName,
        bool allowInsecureHttp = false) =>
        new(
            new TestHostEnvironment(environmentName),
            Options.Create(new BffRuntimeOptions { AllowInsecureHttpForDevelopment = allowInsecureHttp }));

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Altivy.Bff.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
