using Nodra.Backend.Application.Notes;
using Nodra.Backend.Domain.Notes;
using Xunit;

namespace Nodra.Backend.Application.UnitTests.Notes;

[Trait("Category", "Unit")]
public sealed class NoteDocumentTests
{
    [Fact]
    public void MetadataIsDerivedWithoutChangingCanonicalContent()
    {
        var note = new NoteDocument(NotePath.Create("plano.md"), "# Plano\nVeja [[Arquitetura]].", "etag");

        Assert.Equal("# Plano\nVeja [[Arquitetura]].", note.Content);
        Assert.Equal(["Plano"], note.Metadata.Headings);
        Assert.Equal(["Arquitetura"], note.Metadata.WikiLinks);
    }
}
