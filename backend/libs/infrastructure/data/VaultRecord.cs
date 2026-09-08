using Altivy.Backend.Domain.Vaults;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Modelo de persistência da projeção de um vault.
/// </summary>
public sealed class VaultRecord
{
    /// <summary>
    /// Obtém ou define o identificador estável do vault.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o nome exibido do vault.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o estado operacional do vault.
    /// </summary>
    public VaultLifecycleState State { get; set; } = VaultLifecycleState.Ready;
}
