namespace Altivy.Bff.Configuration;

/// <summary>
/// Define parâmetros operacionais configuráveis do BFF.
/// </summary>
public sealed class BffRuntimeOptions
{
    /// <summary>
    /// Nome da seção de configuração dos parâmetros operacionais do BFF.
    /// </summary>
    public const string SectionName = "Bff";

    /// <summary>
    /// Nome usado para isolar as chaves de Data Protection da aplicação.
    /// </summary>
    public string DataProtectionApplicationName { get; init; } = "Altivy.Bff";

    /// <summary>
    /// Permite HTTP não loopback exclusivamente no ambiente Development quando configurado explicitamente.
    /// </summary>
    public bool AllowInsecureHttpForDevelopment { get; init; }

    /// <summary>
    /// Quantidade máxima de entradas mantidas no cache de memória.
    /// </summary>
    public long MemoryCacheSizeLimit { get; init; } = 10_000;

    /// <summary>
    /// Quantidade de tentativas de login permitidas por janela e endereço remoto.
    /// </summary>
    public int LoginRateLimitPermitLimit { get; init; } = 10;

    /// <summary>
    /// Duração, em segundos, da janela de limitação de login.
    /// </summary>
    public int LoginRateLimitWindowSeconds { get; init; } = 60;

    /// <summary>
    /// Quantidade de requisições de login aguardando na fila do limitador.
    /// </summary>
    public int LoginRateLimitQueueLimit { get; init; }

    /// <summary>
    /// Timeout, em segundos, da chamada de renovação de token no Keycloak.
    /// </summary>
    public int TokenRefreshTimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// Tamanho máximo, em bytes, da resposta de renovação de token.
    /// </summary>
    public long TokenRefreshMaxResponseContentBufferSize { get; init; } = 64 * 1024;

    /// <summary>
    /// Timeout, em segundos, da verificação de descoberta do Keycloak.
    /// </summary>
    public int KeycloakDiscoveryTimeoutSeconds { get; init; } = 3;

    /// <summary>
    /// Timeout, em segundos, das chamadas para a API downstream.
    /// </summary>
    public int DownstreamApiTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Nome do cookie usado para armazenar o token antiforgery.
    /// </summary>
    public string AntiforgeryCookieName { get; init; } = "__Host-Altivy.Antiforgery";

    /// <summary>
    /// Nome do header usado para transportar o token antiforgery.
    /// </summary>
    public string AntiforgeryHeaderName { get; init; } = "X-Altivy-Antiforgery";
}
