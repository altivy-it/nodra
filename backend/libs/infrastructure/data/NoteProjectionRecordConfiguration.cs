using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nodra.Backend.Infra.Data.Postgres;

internal sealed class NoteProjectionRecordConfiguration : IEntityTypeConfiguration<NoteProjectionRecord>
{
    public void Configure(EntityTypeBuilder<NoteProjectionRecord> builder)
    {
        _ = builder.ToTable("note_projections");
        _ = builder.HasKey(record => new { record.VaultId, record.Path });
        _ = builder.Property(record => record.VaultId).HasColumnName("vault_id").HasMaxLength(128);
        _ = builder.Property(record => record.Path).HasColumnName("path").HasMaxLength(1024);
        _ = builder.Property(record => record.ContentHash).HasColumnName("content_hash").HasMaxLength(64).IsRequired();
        _ = builder.Property(record => record.Title).HasColumnName("title").HasMaxLength(1024);
        _ = builder.Property(record => record.PropertiesJson).HasColumnName("properties").HasColumnType("jsonb").IsRequired();
        _ = builder.Property(record => record.TagsJson).HasColumnName("tags").HasColumnType("jsonb").IsRequired();
        _ = builder.Property(record => record.HeadingsJson).HasColumnName("headings").HasColumnType("jsonb").IsRequired();
        _ = builder.Property(record => record.WikiLinksJson).HasColumnName("wiki_links").HasColumnType("jsonb").IsRequired();
        _ = builder.Property(record => record.MarkdownLinksJson).HasColumnName("markdown_links").HasColumnType("jsonb").IsRequired();
        _ = builder.Property(record => record.IndexedAt).HasColumnName("indexed_at").IsRequired();
        _ = builder.HasIndex(record => new { record.VaultId, record.Title });
    }
}
