using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Mapeia operações de provisioning no schema PostgreSQL do Altivy.
/// </summary>
public sealed class VaultProvisioningOperationRecordConfiguration : IEntityTypeConfiguration<VaultProvisioningOperationRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<VaultProvisioningOperationRecord> builder)
    {
        builder.ToTable("vault_provisioning_operations");
        builder.HasKey(record => record.OperationId);
        builder.Property(record => record.OperationId).HasColumnName("operation_id");
        builder.Property(record => record.VaultId).HasColumnName("vault_id").HasMaxLength(64).IsRequired();
        builder.Property(record => record.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(record => record.Subject).HasColumnName("subject").HasMaxLength(255).IsRequired();
        builder.Property(record => record.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        builder.Property(record => record.State).HasColumnName("state").IsRequired();
        builder.Property(record => record.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(record => record.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(record => record.LastError).HasColumnName("last_error").HasMaxLength(2000);
        builder.Property(record => record.LeaseId).HasColumnName("lease_id");
        builder.Property(record => record.LeaseExpiresAt).HasColumnName("lease_expires_at");
        builder.HasIndex(record => new { record.Subject, record.IdempotencyKey }).IsUnique();
        builder.HasIndex(record => new { record.State, record.NextAttemptAt });
        builder.HasIndex(record => new { record.State, record.LeaseExpiresAt });
        builder.HasIndex(record => record.VaultId);
    }
}
