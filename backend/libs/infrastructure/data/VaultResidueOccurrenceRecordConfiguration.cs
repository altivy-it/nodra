using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Mapeia ocorrências operacionais de resíduos de vault no schema PostgreSQL.
/// </summary>
public sealed class VaultResidueOccurrenceRecordConfiguration : IEntityTypeConfiguration<VaultResidueOccurrenceRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<VaultResidueOccurrenceRecord> builder)
    {
        builder.ToTable("vault_residue_occurrences");
        builder.HasKey(record => record.OccurrenceId);
        builder.Property(record => record.OccurrenceId).HasColumnName("occurrence_id");
        builder.Property(record => record.VaultId).HasColumnName("vault_id").HasMaxLength(64).IsRequired();
        builder.Property(record => record.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        builder.Property(record => record.FirstDetectedAt).HasColumnName("first_detected_at").IsRequired();
        builder.Property(record => record.LastDetectedAt).HasColumnName("last_detected_at").IsRequired();
        builder.Property(record => record.AlertPending).HasColumnName("alert_pending").IsRequired();
        builder.Property(record => record.AlertAttemptCount).HasColumnName("alert_attempt_count").IsRequired();
        builder.Property(record => record.NextAlertAttemptAt).HasColumnName("next_alert_attempt_at");
        builder.Property(record => record.AlertLeaseId).HasColumnName("alert_lease_id");
        builder.Property(record => record.AlertLeaseExpiresAt).HasColumnName("alert_lease_expires_at");
        builder.Property(record => record.AlertLastError).HasColumnName("alert_last_error").HasMaxLength(2000);
        builder.Property(record => record.AlertActionRequired).HasColumnName("alert_action_required").HasMaxLength(64).IsRequired();
        builder.Property(record => record.RetentionWarningScheduledAt).HasColumnName("retention_warning_scheduled_at");
        builder.HasIndex(record => new { record.VaultId, record.Type }).IsUnique();
        builder.HasIndex(record => new { record.AlertPending, record.NextAlertAttemptAt });
        builder.HasIndex(record => new { record.AlertPending, record.AlertLeaseExpiresAt });
        builder.HasIndex(record => new { record.RetentionWarningScheduledAt, record.LastDetectedAt });
    }
}