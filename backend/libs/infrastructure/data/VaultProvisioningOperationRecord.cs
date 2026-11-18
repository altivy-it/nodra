namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Modelo de persistência de uma operação idempotente de provisioning.
/// </summary>
public sealed class VaultProvisioningOperationRecord
{
    /// <summary>Obtém ou define o identificador da operação.</summary>
    public Guid OperationId { get; set; }

    /// <summary>Obtém ou define o instante UTC de criação da operação.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Obtém ou define o identificador do vault.</summary>
    public string VaultId { get; set; } = string.Empty;

    /// <summary>Obtém ou define o nome do vault.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Obtém ou define o sujeito proprietário.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Obtém ou define a chave de idempotência.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Obtém ou define o estado serializado do provisioning.</summary>
    public int State { get; set; }

    /// <summary>Obtém ou define o número de tentativas do job.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Obtém ou define a data da próxima tentativa.</summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>Obtém ou define o erro sanitizado da última tentativa.</summary>
    public string? LastError { get; set; }

    /// <summary>Obtém ou define o identificador do lease ativo.</summary>
    public Guid? LeaseId { get; set; }

    /// <summary>Obtém ou define a expiração do lease ativo.</summary>
    public DateTimeOffset? LeaseExpiresAt { get; set; }
}
