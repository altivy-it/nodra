namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Conteúdo mínimo e não sensível de um alerta administrativo de resíduo.
/// </summary>
/// <param name="occurrenceId">Identificador técnico da ocorrência.</param>
/// <param name="vaultId">Identificador opaco do vault afetado.</param>
/// <param name="type">Tipo versionado do resíduo.</param>
/// <param name="firstDetectedAt">Primeiro instante UTC de detecção.</param>
/// <param name="lastDetectedAt">Último instante UTC de detecção.</param>
/// <param name="attemptCount">Tentativa de entrega em execução.</param>
/// <param name="actionRequired">Ação administrativa requerida.</param>
public sealed record VaultResidueAlert(
    Guid OccurrenceId,
    string VaultId,
    string Type,
    DateTimeOffset FirstDetectedAt,
    DateTimeOffset LastDetectedAt,
    int AttemptCount,
    string ActionRequired);
