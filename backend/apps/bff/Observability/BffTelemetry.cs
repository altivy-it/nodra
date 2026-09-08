using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Altivy.Bff.Observability;

/// <summary>
/// Publica métricas operacionais do BFF com atributos de cardinalidade limitada.
/// </summary>
public sealed class BffTelemetry : IDisposable
{
    /// <summary>Nome estável do meter do BFF.</summary>
    public const string MeterName = "Altivy.Bff";

    private readonly Meter meter = new(MeterName);
    private readonly Counter<long> refreshAttempts;
    private readonly Histogram<double> refreshDuration;

    /// <summary>Inicializa os instrumentos de renovação de sessão.</summary>
    public BffTelemetry()
    {
        refreshAttempts = meter.CreateCounter<long>(
            "altivy.bff.token_refresh.attempts",
            description: "Quantidade de tentativas de renovação de token por resultado.");
        refreshDuration = meter.CreateHistogram<double>(
            "altivy.bff.token_refresh.duration",
            unit: "s",
            description: "Duração da tentativa de renovação de token.");
    }

    /// <summary>Registra resultado e duração de uma tentativa de renovação.</summary>
    /// <param name="duration">Duração observada.</param>
    /// <param name="outcome">Resultado da tentativa.</param>
    public void RecordTokenRefresh(TimeSpan duration, TokenRefreshOutcome outcome)
    {
        var result = outcome switch
        {
            TokenRefreshOutcome.Success => "success",
            TokenRefreshOutcome.Rejected => "rejected",
            TokenRefreshOutcome.TransientFailure => "transient_failure",
            TokenRefreshOutcome.RefreshTokenMissing => "refresh_token_missing",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };
        var tags = new TagList { { "result", result } };
        refreshAttempts.Add(1, tags);
        refreshDuration.Record(duration.TotalSeconds, tags);
    }

    /// <inheritdoc />
    public void Dispose() => meter.Dispose();
}