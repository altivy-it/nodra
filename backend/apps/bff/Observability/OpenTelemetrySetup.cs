using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Nodra.Bff.Observability;

/// <summary>
/// Registra instrumentação e exportação OpenTelemetry do BFF.
/// </summary>
public static class OpenTelemetrySetup
{
    /// <summary>
    /// Adiciona métricas e traces do ASP.NET Core, HttpClient, runtime e BFF.
    /// </summary>
    /// <param name="services">Coleção de serviços do host.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A mesma coleção de serviços.</returns>
    public static IServiceCollection AddBffOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Sdk.SetDefaultTextMapPropagator(new TraceContextPropagator());

        var section = configuration.GetSection(OpenTelemetryOptions.SectionName);
        _ = services.AddOptions<OpenTelemetryOptions>()
            .Bind(section)
            .ValidateOnStart();
        _ = services.AddSingleton<IValidateOptions<OpenTelemetryOptions>, OpenTelemetryOptionsValidator>();
        _ = services.AddSingleton<BffTelemetry>();

        var options = section.Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();
        _ = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("altivy-bff"))
            .WithTracing(tracing =>
            {
                _ = tracing
                    .AddAspNetCoreInstrumentation(instrumentation =>
                        instrumentation.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation();
                if (TryGetEndpoint(options, out var endpoint))
                {
                    _ = tracing.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
                }
            })
            .WithMetrics(metrics =>
            {
                _ = metrics
                    .AddMeter(BffTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
                if (TryGetEndpoint(options, out var endpoint))
                {
                    _ = metrics.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
                }
            });

        return services;
    }

    private static bool TryGetEndpoint(OpenTelemetryOptions options, out Uri endpoint) =>
        Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out endpoint!);
}
