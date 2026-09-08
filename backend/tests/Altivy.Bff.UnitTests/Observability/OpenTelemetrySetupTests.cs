using Altivy.Bff.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Context.Propagation;

namespace Altivy.Bff.UnitTests.Observability;

[Trait("Category", "Unit")]
public sealed class OpenTelemetrySetupTests
{
    [Fact]
    public void AddBffOpenTelemetryDoesNotPropagateBaggage()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddBffOpenTelemetry(configuration);

        var fields = Propagators.DefaultTextMapPropagator.Fields;
        Assert.NotNull(fields);
        Assert.Contains("traceparent", fields);
        Assert.Contains("tracestate", fields);
        Assert.DoesNotContain("baggage", fields);
    }
}