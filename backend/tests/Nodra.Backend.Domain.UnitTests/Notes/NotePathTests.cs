using Nodra.Backend.Domain.Notes;
using Xunit;

namespace Nodra.Backend.Domain.UnitTests.Notes;

[Trait("Category", "Unit")]
public sealed class NotePathTests
{
    [Fact]
    public void CreateWithRelativeMarkdownPathNormalizesSeparators()
    {
        var path = NotePath.Create(" projetos\\plano.md ");

        Assert.Equal("projetos/plano.md", path.Value);
    }

    [Theory]
    [InlineData("../segredo.md")]
    [InlineData("/segredo.md")]
    [InlineData("C:\\segredo.md")]
    [InlineData("nota.txt")]
    [InlineData("pasta//nota.md")]
    [InlineData(".altivy-trash/nota.md")]
    [InlineData("Projetos/.ALTIVY-TRASH/nota.md")]
    public void CreateWithUnsafeOrNonMarkdownPathThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => NotePath.Create(value));
    }

}