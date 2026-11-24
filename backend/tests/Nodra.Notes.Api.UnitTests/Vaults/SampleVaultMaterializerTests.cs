using Microsoft.Extensions.Logging.Abstractions;
using Nodra.Backend.Application.Vaults;
using Nodra.Notes.Api.Vaults;
using Xunit;

namespace Nodra.Notes.Api.UnitTests.Vaults;

[Trait("Category", "Integration")]
public sealed class SampleVaultMaterializerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"nodra-sample-{Guid.NewGuid():N}");
    private readonly SampleVaultMaterializer materializer;

    public SampleVaultMaterializerTests() =>
        materializer = new SampleVaultMaterializer(root, NullLogger<SampleVaultMaterializer>.Instance);

    private string DocumentPath => Path.Combine(root, SampleVault.Id, SampleVault.DocumentPath);

    [Fact]
    public async Task MaterializeCreatesTheOriginalDocumentWhenAbsent()
    {
        var written = await materializer.MaterializeAsync(CancellationToken.None);

        Assert.True(written);
        Assert.Equal(await SampleVaultMaterializer.ReadDocumentAsync(CancellationToken.None), await File.ReadAllBytesAsync(DocumentPath));
        _ = Assert.Single(Directory.GetFiles(Path.Combine(root, SampleVault.Id)));
    }

    [Fact]
    public async Task MaterializeRestoresADivergentDocumentAndKeepsAnIdenticalOne()
    {
        _ = await materializer.MaterializeAsync(CancellationToken.None);
        await File.WriteAllTextAsync(DocumentPath, "# Alterado fora do Nodra");

        var restored = await materializer.MaterializeAsync(CancellationToken.None);
        var unchanged = await materializer.MaterializeAsync(CancellationToken.None);

        Assert.True(restored);
        Assert.False(unchanged);
        Assert.Contains("Documento modelo do Nodra", await File.ReadAllTextAsync(DocumentPath), StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
