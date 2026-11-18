using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nodra.Bff.Observability;
using OpenTelemetry.Context.Propagation;

namespace Nodra.Bff.UnitTests.Observability;

[Trait("Category", "Unit")]
public sealed class OpenTelemetrySetupTests
{
    [Fact]
    public void AddBffOpenTelemetryDoesNotPropagateBaggage()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        _ = services.AddBffOpenTelemetry(configuration);

        var fields = Propagators.DefaultTextMapPropagator.Fields;
        Assert.NotNull(fields);
        Assert.Contains("traceparent", fields);
        Assert.Contains("tracestate", fields);
        Assert.DoesNotContain("baggage", fields);
    }
}
