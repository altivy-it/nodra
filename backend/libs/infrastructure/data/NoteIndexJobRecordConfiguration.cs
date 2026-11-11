using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nodra.Backend.Infra.Data.Postgres;

internal sealed class NoteIndexJobRecordConfiguration : IEntityTypeConfiguration<NoteIndexJobRecord>
{
    public void Configure(EntityTypeBuilder<NoteIndexJobRecord> builder)
    {
        builder.ToTable("note_index_jobs");
        builder.HasKey(record => new { record.JobId, record.CreatedAtUtc });
        builder.Property(record => record.JobId).HasColumnName("job_id");
        builder.Property(record => record.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(record => record.VaultId).HasColumnName("vault_id").HasMaxLength(128).IsRequired();
        builder.Property(record => record.Path).HasColumnName("path").HasMaxLength(1024);
        builder.Property(record => record.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(256).IsRequired();
        builder.Property(record => record.State).HasColumnName("state").IsRequired();
        builder.Property(record => record.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(record => record.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(record => record.LastError).HasColumnName("last_error").HasMaxLength(256);
        builder.Property(record => record.LeaseId).HasColumnName("lease_id");
        builder.Property(record => record.LeaseExpiresAt).HasColumnName("lease_expires_at");
        builder.HasIndex(record => new { record.IdempotencyKey, record.CreatedAtUtc }).IsUnique();
        builder.HasIndex(record => new { record.State, record.NextAttemptAt });
        builder.HasIndex(record => new { record.State, record.LeaseExpiresAt });
    }
}
