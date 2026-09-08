using Altivy.Backend.Domain.Vaults;

namespace Altivy.Backend.Application.Vaults;

/// <summary>
/// Resultado da solicitação de criação de vault.
/// </summary>
public sealed record CreateVaultResult(
    Guid OperationId,
    string VaultId,
    VaultLifecycleState State,
    bool Reused);