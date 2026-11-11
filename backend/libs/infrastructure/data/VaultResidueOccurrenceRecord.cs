namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Registra uma divergência operacional que requer alerta e recuperação administrativa.
/// </summary>
public sealed class VaultResidueOccurrenceRecord
{
    /// <summary>Obtém ou define o identificador técnico da ocorrência.</summary>
    public Guid OccurrenceId { get; set; }

    /// <summary>Obtém ou define o identificador opaco do vault afetado.</summary>
    public string VaultId { get; set; } = string.Empty;

    /// <summary>Obtém ou define o tipo versionado de resíduo.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Obtém ou define o primeiro instante UTC de detecção.</summary>
    public DateTimeOffset FirstDetectedAt { get; set; }

    /// <summary>Obtém ou define o último instante UTC de detecção.</summary>
    public DateTimeOffset LastDetectedAt { get; set; }

    /// <summary>Obtém ou define se o alerta administrativo ainda deve ser entregue.</summary>
    public bool AlertPending { get; set; }

    /// <summary>Obtém ou define o número de tentativas de entrega do alerta.</summary>
    public int AlertAttemptCount { get; set; }

    /// <summary>Obtém ou define o próximo instante UTC de tentativa de entrega.</summary>
    public DateTimeOffset? NextAlertAttemptAt { get; set; }

    /// <summary>Obtém ou define o identificador do lease de entrega ativo.</summary>
    public Guid? AlertLeaseId { get; set; }

    /// <summary>Obtém ou define a expiração UTC do lease de entrega ativo.</summary>
    public DateTimeOffset? AlertLeaseExpiresAt { get; set; }

    /// <summary>Obtém ou define o código sanitizado do último erro de entrega.</summary>
    public string? AlertLastError { get; set; }

    /// <summary>Obtém ou define a ação administrativa do alerta pendente.</summary>
    public string AlertActionRequired { get; set; } = string.Empty;

    /// <summary>Obtém ou define o instante UTC do aviso de retenção vencida.</summary>
    public DateTimeOffset? RetentionWarningScheduledAt { get; set; }
}