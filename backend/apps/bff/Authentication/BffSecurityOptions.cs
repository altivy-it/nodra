namespace Altivy.Bff.Authentication;

/// <summary>
/// Define as configurações de proteção de dados, origens permitidas e proxies confiáveis do BFF.
/// </summary>
public sealed class BffSecurityOptions
{
    /// <summary>
    /// Nome da seção de configuração que contém as opções de segurança.
    /// </summary>
    public const string SectionName = "Security";

    /// <summary>
    /// Caminho onde as chaves de Data Protection são persistidas.
    /// </summary>
    public string DataProtectionKeysPath { get; init; } = string.Empty;

    /// <summary>
    /// Caminho do certificado usado para proteger as chaves de Data Protection.
    /// </summary>
    public string DataProtectionCertificatePath { get; init; } = string.Empty;

    /// <summary>
    /// Senha do certificado usado para proteger as chaves de Data Protection.
    /// </summary>
    public string DataProtectionCertificatePassword { get; init; } = string.Empty;

    /// <summary>
    /// Origens que podem realizar requisições mutáveis ao BFF.
    /// </summary>
    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];

    /// <summary>
    /// Endereços IP dos proxies autorizados a encaminhar headers para o BFF.
    /// </summary>
    public IReadOnlyList<string> TrustedProxies { get; init; } = [];

    /// <summary>
    /// Redes IP autorizadas a encaminhar headers para o BFF em formato CIDR.
    /// </summary>
    public IReadOnlyList<string> TrustedNetworks { get; init; } = [];
}
