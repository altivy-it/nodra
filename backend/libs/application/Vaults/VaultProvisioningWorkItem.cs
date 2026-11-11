using Nodra.Backend.Domain.Vaults;

namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Representa uma operação de provisioning reivindicada exclusivamente por um processador.
/// </summary>
/// <param name="operation">Operação cujo estado está em provisioning.</param>
/// <param name="leaseId">Identificador opaco da reivindicação.</param>
/// <param name="attemptCount">Número da tentativa em execução.</param>
public sealed record VaultProvisioningWorkItem(
    VaultProvisioningOperation Operation,
    Guid LeaseId,
    int AttemptCount);
