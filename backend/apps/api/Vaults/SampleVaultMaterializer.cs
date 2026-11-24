using Nodra.Backend.Application.Vaults;

namespace Nodra.Notes.Api.Vaults;

/// <summary>
/// Garante, na inicialização, que o documento modelo do vault de exemplo seja idêntico ao conteúdo versionado.
/// </summary>
public sealed partial class SampleVaultMaterializer : IHostedService
{
    internal const string ResourceName = "Nodra.Notes.Api.SampleVault.documento-modelo.md";

    private readonly string vaultRoot;
    private readonly ILogger<SampleVaultMaterializer> logger;

    /// <summary>
    /// Cria o materializador para a raiz de vaults configurada.
    /// </summary>
    /// <param name="vaultRoot">Diretório raiz dos vaults.</param>
    /// <param name="logger">Logger da operação.</param>
    public SampleVaultMaterializer(string vaultRoot, ILogger<SampleVaultMaterializer> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vaultRoot);
        this.vaultRoot = Path.GetFullPath(vaultRoot);
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = await MaterializeAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // O vault de exemplo é auxiliar: a API segue atendendo os demais vaults.
            LogMaterializationFailed(logger, exception);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Grava o documento modelo quando ausente ou divergente.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="true"/> quando o arquivo foi gravado.</returns>
    public async Task<bool> MaterializeAsync(CancellationToken cancellationToken)
    {
        var expected = await ReadDocumentAsync(cancellationToken);
        var directory = Path.Combine(vaultRoot, SampleVault.Id);
        var target = Path.Combine(directory, SampleVault.DocumentPath);
        _ = Directory.CreateDirectory(directory);

        var targetInfo = new FileInfo(target);
        if (targetInfo.Exists && targetInfo.LinkTarget is null &&
            (await File.ReadAllBytesAsync(target, cancellationToken)).AsSpan().SequenceEqual(expected))
        {
            return false;
        }

        // Temporário exclusivo + rename atômico: instâncias concorrentes nunca deixam o arquivo parcial.
        var temporary = Path.Combine(directory, $".{SampleVault.DocumentPath}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(temporary, expected, cancellationToken);
            File.Move(temporary, target, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }

        LogMaterialized(logger, SampleVault.Id);
        return true;
    }

    /// <summary>
    /// Lê o conteúdo original do documento modelo embutido na API.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Bytes do documento modelo.</returns>
    public static async Task<byte[]> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        await using var stream = typeof(SampleVaultMaterializer).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Documento modelo não embutido na API.");
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    [LoggerMessage(EventId = 4100, Level = LogLevel.Information, Message = "Documento modelo restaurado no vault {VaultId}.")]
    private static partial void LogMaterialized(ILogger logger, string vaultId);

    [LoggerMessage(EventId = 4101, Level = LogLevel.Error, Message = "Falha ao materializar o vault de exemplo.")]
    private static partial void LogMaterializationFailed(ILogger logger, Exception exception);
}

/// <summary>
/// Registra a materialização do vault de exemplo.
/// </summary>
public static class SampleVaultServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona o serviço que restaura o documento modelo na inicialização.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="vaultRoot">Diretório raiz dos vaults.</param>
    /// <returns>A coleção de serviços.</returns>
    public static IServiceCollection AddSampleVault(this IServiceCollection services, string vaultRoot) =>
        services.AddHostedService(provider => new SampleVaultMaterializer(
            vaultRoot, provider.GetRequiredService<ILogger<SampleVaultMaterializer>>()));
}
