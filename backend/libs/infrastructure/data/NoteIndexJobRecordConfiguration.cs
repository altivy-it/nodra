using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nodra.Backend.Infra.Data.Postgres;

internal sealed class NoteIndexJobRecordConfiguration : IEntityTypeConfiguration<NoteIndexJobRecord>
{
    public void Configure(EntityTypeBuilder<NoteIndexJobRecord> builder)
    {
        _ = builder.ToTable("note_index_jobs");
        _ = builder.HasKey(record => new { record.JobId, record.CreatedAtUtc });
        _ = builder.Property(record => record.JobId).HasColumnName("job_id");
        _ = builder.Property(record => record.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        _ = builder.Property(record => record.VaultId).HasColumnName("vault_id").HasMaxLength(128).IsRequired();
        _ = builder.Property(record => record.Path).HasColumnName("path").HasMaxLength(1024);
        _ = builder.Property(record => record.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(256).IsRequired();
        _ = builder.Property(record => record.State).HasColumnName("state").IsRequired();
        _ = builder.Property(record => record.AttemptCount).HasColumnName("attempt_count").IsRequired();
        _ = builder.Property(record => record.NextAttemptAt).HasColumnName("next_attempt_at");
        _ = builder.Property(record => record.LastError).HasColumnName("last_error").HasMaxLength(256);
        _ = builder.Property(record => record.LeaseId).HasColumnName("lease_id");
        _ = builder.Property(record => record.LeaseExpiresAt).HasColumnName("lease_expires_at");
        _ = builder.HasIndex(record => new { record.IdempotencyKey, record.CreatedAtUtc }).IsUnique();
        _ = builder.HasIndex(record => new { record.State, record.NextAttemptAt });
        _ = builder.HasIndex(record => new { record.State, record.LeaseExpiresAt });
    }
}
