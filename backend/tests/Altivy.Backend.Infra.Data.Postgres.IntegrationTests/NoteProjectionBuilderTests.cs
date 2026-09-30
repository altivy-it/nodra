using System.Globalization;
using Altivy.Backend.Application.Notes;
using Altivy.Backend.Domain.Notes;
using Altivy.Backend.Infra.Data.Postgres;
using Xunit;

namespace Altivy.Backend.Infra.Data.Postgres.IntegrationTests;

[Trait("Category", "Unit")]
public sealed class NoteProjectionBuilderTests
{
    [Fact]
    public void BuildProjectsMetadataWithoutPersistingCanonicalContent()
    {
        var note = new NoteDocument(
            NotePath.Create("projetos/plano.md"),
            "---\ntitle: Plano\ntags: [produto]\n---\n# Plano\nVeja [[Arquitetura]].",
            "etag-ignored");

        var projection = NoteProjectionBuilder.Build(
            "lab-0001",
            note,
            DateTimeOffset.Parse("2026-09-08T12:00:00+00:00", CultureInfo.InvariantCulture));

        Assert.Equal("lab-0001", projection.VaultId);
        Assert.Equal("projetos/plano.md", projection.Path);
        Assert.Equal("Plano", projection.Title);
        Assert.Contains("produto", projection.TagsJson, StringComparison.Ordinal);
        Assert.Contains("Arquitetura", projection.WikiLinksJson, StringComparison.Ordinal);
        Assert.DoesNotContain(note.Content, projection.PropertiesJson, StringComparison.Ordinal);
    }
}