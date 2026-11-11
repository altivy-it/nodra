using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Mapeia a projeção de vault para o schema PostgreSQL do Altivy.
/// </summary>
public sealed class VaultRecordConfiguration : IEntityTypeConfiguration<VaultRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<VaultRecord> builder)
    {
        builder.ToTable("vaults");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(record => record.Name)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(record => record.State)
            .HasColumnName("state")
            .IsRequired();
    }
}
