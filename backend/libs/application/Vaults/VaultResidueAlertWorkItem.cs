namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Alerta durável reivindicado exclusivamente por um processador.
/// </summary>
/// <param name="alert">Conteúdo sanitizado do alerta.</param>
/// <param name="leaseId">Identificador opaco da reivindicação.</param>
public sealed record VaultResidueAlertWorkItem(VaultResidueAlert Alert, Guid LeaseId);
