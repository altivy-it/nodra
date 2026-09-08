namespace Altivy.Backend.Domain.Vaults;

/// <summary>
/// Representa o estado operacional do provisioning de um vault.
/// </summary>
public enum VaultLifecycleState
{
    Pending = 0,
    Provisioning = 1,
    Ready = 2,
    Degraded = 3,
    Archived = 4,
    Failed = 5
}