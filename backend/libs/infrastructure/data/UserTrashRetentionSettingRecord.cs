namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Persiste somente o parâmetro de retenção da lixeira do sujeito autenticado.
/// </summary>
public sealed class UserTrashRetentionSettingRecord
{
    public string Subject { get; set; } = string.Empty;

    public int? RetentionDays { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
