using Nodra.Backend.Domain.Vaults;

namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Resultado da solicitação de criação de vault.
/// </summary>
public sealed record CreateVaultResult(
    Guid OperationId,
    string VaultId,
    VaultLifecycleState State,
    bool Reused);
