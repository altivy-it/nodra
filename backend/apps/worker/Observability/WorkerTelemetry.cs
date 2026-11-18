using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Nodra.Notes.Worker.Observability;

/// <summary>
/// Publica métricas operacionais do worker com atributos de cardinalidade fixa.
/// </summary>
public sealed class WorkerTelemetry : IDisposable
{
    /// <summary>Nome estável do meter do worker.</summary>
    public const string MeterName = "Altivy.Worker";

    private readonly Meter meter = new(MeterName);
    private readonly Counter<long> provisioningProcessed;
    private readonly Counter<long> residuesDetected;
    private readonly Counter<long> alertDeliveries;
    private readonly Counter<long> failures;

    /// <summary>Inicializa os instrumentos operacionais do worker.</summary>
    public WorkerTelemetry()
    {
        provisioningProcessed = meter.CreateCounter<long>("altivy.worker.vault_provisioning.processed");
        residuesDetected = meter.CreateCounter<long>("altivy.worker.vault_residue.detected");
        alertDeliveries = meter.CreateCounter<long>("altivy.worker.vault_residue_alert.processed");
        failures = meter.CreateCounter<long>("altivy.worker.failure");
    }

    /// <summary>Registra uma operação de provisioning processada.</summary>
    public void RecordProvisioningProcessed() => provisioningProcessed.Add(1);

    /// <summary>Registra resíduos detectados por categoria fixa.</summary>
    /// <param name="type">Categoria fixa de resíduo.</param>
    /// <param name="count">Quantidade detectada.</param>
    public void RecordResiduesDetected(string type, int count)
    {
        if (count > 0)
        {
            residuesDetected.Add(count, new TagList { { "type", type } });
        }
    }

    /// <summary>Registra um alerta administrativo processado.</summary>
    public void RecordAlertProcessed() => alertDeliveries.Add(1);

    /// <summary>Registra uma falha operacional por categoria fixa.</summary>
    /// <param name="category">Categoria fixa da falha.</param>
    public void RecordFailure(string category) => failures.Add(1, new TagList { { "category", category } });

    /// <inheritdoc />
    public void Dispose() => meter.Dispose();
}
