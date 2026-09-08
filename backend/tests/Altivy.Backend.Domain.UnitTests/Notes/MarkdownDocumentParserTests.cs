using Altivy.Backend.Domain.Notes;
using Xunit;

namespace Altivy.Backend.Domain.UnitTests.Notes;

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
    public void NoteDocumentExposesDerivedMetadataWithoutChangingCanonicalContent()
    {
        var note = new NoteDocument(NotePath.Create("plano.md"), "# Plano\nVeja [[Arquitetura]].", "etag");

        Assert.Equal("# Plano\nVeja [[Arquitetura]].", note.Content);
        Assert.Equal(["Plano"], note.Metadata.Headings);
        Assert.Equal(["Arquitetura"], note.Metadata.WikiLinks);
    }

    [Fact]
    public void ParseDoesNotTreatRawHtmlOrJavascriptLinkAsSafeOutput()
    {
        var document = MarkdownDocumentParser.Parse("<script>alert(1)</script> [x](javascript:alert(1))");

        Assert.Empty(document.Headings);
        Assert.Empty(document.MarkdownLinks);
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