using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Altivy.Notes.Worker.Observability;

/// <summary>
/// Registra métricas e traces OpenTelemetry opcionais do worker.
/// </summary>
public static class WorkerOpenTelemetrySetup
{
    /// <summary>Adiciona telemetria do worker e exportação OTLP quando configurada.</summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A mesma coleção de serviços.</returns>
    public static IServiceCollection AddWorkerOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var endpointValue = configuration["OpenTelemetry:OtlpEndpoint"];
        Uri? endpoint = Uri.TryCreate(endpointValue, UriKind.Absolute, out var parsedEndpoint)
            ? parsedEndpoint
            : null;
        services.AddSingleton<WorkerTelemetry>();
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("altivy-worker"))
            .WithTracing(tracing =>
            {
                tracing.AddHttpClientInstrumentation();
                if (endpoint is not null)
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(WorkerTelemetry.MeterName)
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
                if (endpoint is not null)
                {
                    metrics.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
                }
            });
        return services;
    }
}