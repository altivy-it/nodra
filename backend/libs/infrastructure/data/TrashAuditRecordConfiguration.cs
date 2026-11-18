using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Mapeia a trilha append-only de operações de lixeira.
/// </summary>
public sealed class TrashAuditRecordConfiguration : IEntityTypeConfiguration<TrashAuditRecord>
{
    public void Configure(EntityTypeBuilder<TrashAuditRecord> builder)
    {
        _ = builder.ToTable("trash_audit_events");
        _ = builder.HasKey(record => record.AuditId);
        _ = builder.Property(record => record.AuditId).HasColumnName("audit_id");
        _ = builder.Property(record => record.OperationId).HasColumnName("operation_id").IsRequired();
        _ = builder.Property(record => record.VaultId).HasColumnName("vault_id").HasMaxLength(64).IsRequired();
        _ = builder.Property(record => record.Subject).HasColumnName("subject").HasMaxLength(512).IsRequired();
        _ = builder.Property(record => record.Action).HasColumnName("action").HasMaxLength(64).IsRequired();
        _ = builder.Property(record => record.Outcome).HasColumnName("outcome").HasMaxLength(64).IsRequired();
        _ = builder.Property(record => record.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        _ = builder.Property(record => record.ReauthenticatedAtUtc).HasColumnName("reauthenticated_at_utc");
        _ = builder.HasIndex(record => new { record.VaultId, record.OccurredAtUtc });
        _ = builder.HasIndex(record => new { record.Subject, record.OccurredAtUtc });
    }
}
