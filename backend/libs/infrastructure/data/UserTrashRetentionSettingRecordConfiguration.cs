using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Mapeia o parâmetro de retenção isolado por sujeito.
/// </summary>
public sealed class UserTrashRetentionSettingRecordConfiguration : IEntityTypeConfiguration<UserTrashRetentionSettingRecord>
{
    public void Configure(EntityTypeBuilder<UserTrashRetentionSettingRecord> builder)
    {
        builder.ToTable("user_trash_retention_settings");
        builder.HasKey(record => record.Subject);
        builder.Property(record => record.Subject).HasColumnName("subject").HasMaxLength(512);
        builder.Property(record => record.RetentionDays).HasColumnName("retention_days");
        builder.Property(record => record.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_user_trash_retention_settings_days",
            "retention_days IS NULL OR (retention_days >= 1 AND retention_days <= 3650)"));
    }
}
