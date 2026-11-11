using Nodra.Backend.Domain.Notes;
using Xunit;

namespace Nodra.Backend.Domain.UnitTests.Notes;

[Trait("Category", "Unit")]
public sealed class FolderPathTests
{
    [Theory]
    [InlineData(".altivy-trash")]
    [InlineData("projetos/.ALTIVY-TRASH")]
    public void CreateRejectsReservedTrashDirectory(string value)
    {
        Assert.Throws<ArgumentException>(() => FolderPath.Create(value));
    }
}