using Nodra.Backend.Domain.Notes;
using Xunit;

namespace Nodra.Backend.Application.UnitTests.Notes;

[Trait("Category", "Unit")]
public sealed class FolderPathTests
{
    [Fact]
    public void CreateWithRelativePathNormalizesSeparators()
    {
        var path = FolderPath.Create(" projetos\\arquitetura ");

        Assert.Equal("projetos/arquitetura", path.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../segredo")]
    [InlineData("/segredo")]
    [InlineData("C:\\segredo")]
    [InlineData("pasta//filha")]
    [InlineData("nome.")]
    [InlineData("CON.txt")]
    [InlineData("pasta/nome?")]
    public void CreateWithUnsafePathThrowsArgumentException(string value)
    {
        _ = Assert.Throws<ArgumentException>(() => FolderPath.Create(value));
    }
}
