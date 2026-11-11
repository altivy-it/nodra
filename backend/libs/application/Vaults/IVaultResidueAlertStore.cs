namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Porta para reivindicar e concluir alertas administrativos de resíduos.
/// </summary>
public interface IVaultResidueAlertStore
{
    /// <summary>Reivindica o próximo alerta pendente de entrega.</summary>
    /// <param name="now">Instante atual em UTC.</param>
    /// <param name="leaseDuration">Duração máxima da reivindicação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O alerta reivindicado ou <see langword="null"/> sem trabalho elegível.</returns>
    Task<VaultResidueAlertWorkItem?> ClaimNextAlertAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>Conclui ou agenda novamente um alerta reivindicado.</summary>
    /// <param name="workItem">Alerta e lease que identificam o processamento.</param>
    /// <param name="nextAttemptAt">Próxima tentativa em caso de falha.</param>
    /// <param name="lastError">Código sanitizado de falha, quando aplicável.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="true"/> quando o lease ainda pertencia ao processador.</returns>
    Task<bool> CompleteAlertAsync(
        VaultResidueAlertWorkItem workItem,
        DateTimeOffset? nextAttemptAt,
        string? lastError,
        CancellationToken cancellationToken);

    /// <summary>Agenda avisos para ocorrências cujo prazo de retenção venceu.</summary>
    /// <param name="now">Instante atual em UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de avisos agendados.</returns>
    Task<int> ScheduleRetentionWarningsAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
