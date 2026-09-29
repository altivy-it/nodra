using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Mapeia a trilha append-only de operações de lixeira.
/// </summary>
public sealed class TrashAuditRecordConfiguration : IEntityTypeConfiguration<TrashAuditRecord>
{
    public void Configure(EntityTypeBuilder<TrashAuditRecord> builder)
    {
        builder.ToTable("trash_audit_events");
        builder.HasKey(record => record.AuditId);
        builder.Property(record => record.AuditId).HasColumnName("audit_id");
        builder.Property(record => record.OperationId).HasColumnName("operation_id").IsRequired();
        builder.Property(record => record.VaultId).HasColumnName("vault_id").HasMaxLength(64).IsRequired();
        builder.Property(record => record.Subject).HasColumnName("subject").HasMaxLength(512).IsRequired();
        builder.Property(record => record.Action).HasColumnName("action").HasMaxLength(64).IsRequired();
        builder.Property(record => record.Outcome).HasColumnName("outcome").HasMaxLength(64).IsRequired();
        builder.Property(record => record.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        builder.Property(record => record.ReauthenticatedAtUtc).HasColumnName("reauthenticated_at_utc");
        builder.HasIndex(record => new { record.VaultId, record.OccurredAtUtc });
        builder.HasIndex(record => new { record.Subject, record.OccurredAtUtc });
    }
}
