namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Persiste o parâmetro não autorizativo de retenção por sujeito.
/// </summary>
public interface ITrashRetentionSettingsStore
{
    Task<int?> GetRetentionDaysAsync(string subject, CancellationToken cancellationToken);

    Task SetRetentionDaysAsync(string subject, int? retentionDays, CancellationToken cancellationToken);
}
