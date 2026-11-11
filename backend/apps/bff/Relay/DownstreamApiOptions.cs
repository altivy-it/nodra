namespace Nodra.Bff.Relay;

/// <summary>
/// Define as configurações da API downstream acessada pelo BFF.
/// </summary>
public sealed class DownstreamApiOptions
{
    /// <summary>
    /// Nome da seção de configuração da API downstream.
    /// </summary>
    public const string SectionName = "Downstream:Api";

    /// <summary>
    /// URL base absoluta da API downstream.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;
}