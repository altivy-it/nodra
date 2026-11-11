using Nodra.Bff.Observability;
using Microsoft.Extensions.FileProviders;

namespace Nodra.Bff.UnitTests.Observability;

[Trait("Category", "Unit")]
public sealed class OpenTelemetryOptionsValidatorTests
{
    [Fact]
    public void ValidateWithoutExporterSucceeds()
    {
        var validator = new OpenTelemetryOptionsValidator(new TestHostEnvironment("Production"));

        var result = validator.Validate(null, new OpenTelemetryOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithHttpsExporterSucceeds()
    {
        var validator = new OpenTelemetryOptionsValidator(new TestHostEnvironment("Production"));
        var options = new OpenTelemetryOptions { OtlpEndpoint = "https://otel.example:4317" };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithRemoteHttpExporterFails()
    {
        var validator = new OpenTelemetryOptionsValidator(new TestHostEnvironment("Development"));
        var options = new OpenTelemetryOptions { OtlpEndpoint = "http://otel.example:4317" };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void ValidateWithLoopbackHttpExporterInDevelopmentSucceeds()
    {
        var validator = new OpenTelemetryOptionsValidator(new TestHostEnvironment("Development"));
        var options = new OpenTelemetryOptions { OtlpEndpoint = "http://localhost:4317" };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithQueryOrFragmentFails()
    {
        var validator = new OpenTelemetryOptionsValidator(new TestHostEnvironment("Production"));
        var options = new OpenTelemetryOptions { OtlpEndpoint = "https://otel.example:4317?token=synthetic" };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Nodra.Bff.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}