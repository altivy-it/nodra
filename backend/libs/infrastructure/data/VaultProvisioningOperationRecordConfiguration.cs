using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Mapeia operações de provisioning no schema PostgreSQL do Altivy.
/// </summary>
public sealed class VaultProvisioningOperationRecordConfiguration : IEntityTypeConfiguration<VaultProvisioningOperationRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<VaultProvisioningOperationRecord> builder)
    {
        _ = builder.ToTable("vault_provisioning_operations");
        _ = builder.HasKey(record => new { record.OperationId, record.CreatedAtUtc });
        _ = builder.Property(record => record.OperationId).HasColumnName("operation_id");
        _ = builder.Property(record => record.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        _ = builder.Property(record => record.VaultId).HasColumnName("vault_id").HasMaxLength(64).IsRequired();
        _ = builder.Property(record => record.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        _ = builder.Property(record => record.Subject).HasColumnName("subject").HasMaxLength(255).IsRequired();
        _ = builder.Property(record => record.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        _ = builder.Property(record => record.State).HasColumnName("state").IsRequired();
        _ = builder.Property(record => record.AttemptCount).HasColumnName("attempt_count").IsRequired();
        _ = builder.Property(record => record.NextAttemptAt).HasColumnName("next_attempt_at");
        _ = builder.Property(record => record.LastError).HasColumnName("last_error").HasMaxLength(2000);
        _ = builder.Property(record => record.LeaseId).HasColumnName("lease_id");
        _ = builder.Property(record => record.LeaseExpiresAt).HasColumnName("lease_expires_at");
        _ = builder.HasIndex(record => new { record.Subject, record.IdempotencyKey, record.CreatedAtUtc }).IsUnique();
        _ = builder.HasIndex(record => new { record.State, record.NextAttemptAt });
        _ = builder.HasIndex(record => new { record.State, record.LeaseExpiresAt });
        _ = builder.HasIndex(record => record.VaultId);
    }
}
