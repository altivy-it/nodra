using Nodra.Backend.Domain.Notes;
using Xunit;

namespace Nodra.Backend.Domain.UnitTests.Notes;

[Trait("Category", "Unit")]
public sealed class MarkdownDocumentParserTests
{
    [Fact]
    public void ParseExtractsPropertiesTagsHeadingsAndLinks()
    {
        const string content = "---\n" +
            "title: \"Plano\"\n" +
            "tags: [produto, arquitetura]\n" +
            "audience: interno\n" +
            "---\n" +
            "# Plano\n" +
            "## Decisões\n" +
            "Veja [[Arquitetura]] e [documentação](docs/guia.md).";

        var document = MarkdownDocumentParser.Parse(content);

        Assert.Equal("Plano", document.Properties["title"]);
        Assert.Equal(["produto", "arquitetura"], document.Tags);
        Assert.Equal(["Plano", "Decisões"], document.Headings);
        Assert.Equal(["Arquitetura"], document.WikiLinks);
        Assert.Equal(["docs/guia.md"], document.MarkdownLinks);
    }

    [Fact]
    public void ParseDoesNotTreatRawHtmlOrJavascriptLinkAsSafeOutput()
    {
        var document = MarkdownDocumentParser.Parse("<script>alert(1)</script> [x](javascript:alert(1))");

        Assert.Empty(document.Headings);
        Assert.Empty(document.MarkdownLinks);
    }

    [Fact]
    public void ParseExtractsNamespacedNodraMetadata()
    {
        const string content = "---\n" +
            "title: \"Plano\"\n" +
            "altivy:\n" +
            "  metadata_version: \"1.0\"\n" +
            "  classification: unclassified\n" +
            "  doc_type: note\n" +
            "  tags: [inbox]\n" +
            "---\n" +
            "# Plano\n";

        var document = MarkdownDocumentParser.Parse(content);

        Assert.Equal("1.0", document.Properties["altivy.metadata_version"]);
        Assert.Equal("unclassified", document.Properties["altivy.classification"]);
        Assert.Equal("note", document.Properties["altivy.doc_type"]);
        Assert.Equal(["inbox"], document.Tags);
    }

    [Fact]
    public void ParseDetectsUnresolvedLocalLinksAgainstKnownVaultPaths()
    {
        const string content = "# Plano\nVeja [[Arquitetura]] e [guia](docs/guia.md).";
        var document = MarkdownDocumentParser.Parse(content);
        var knownPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "notas/novo.md",
            "docs/outro.md"
        };

        Assert.True(document.HasUnresolvedLinks("notas/plano.md", knownPaths));

        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "notas/Arquitetura.md",
            "docs/guia.md"
        };

        Assert.False(document.HasUnresolvedLinks("notas/plano.md", resolved));
    }

    [Theory]
    [InlineData("---\ntitle: um\ntitle: dois\n---\ntexto")]
    [InlineData("---\ntitle\n---\ntexto")]
    [InlineData("---\ntitle: um")]
    public void ParseRejectsInvalidFrontmatter(string content)
    {
        Assert.Throws<ArgumentException>(() => MarkdownDocumentParser.Parse(content));
    }
}
