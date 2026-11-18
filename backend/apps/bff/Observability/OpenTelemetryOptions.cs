namespace Nodra.Bff.Observability;

/// <summary>
/// Configura a exportação opcional de métricas e traces por OTLP.
/// </summary>
public sealed class OpenTelemetryOptions
{
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>Obtém o endpoint base do collector OTLP.</summary>
    public string OtlpEndpoint { get; init; } = string.Empty;
}
