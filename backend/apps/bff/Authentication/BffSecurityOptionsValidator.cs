using Microsoft.Extensions.Options;
using System.Net;

namespace Nodra.Bff.Authentication;

/// <summary>
/// Valida as configurações de segurança do BFF antes da aplicação iniciar.
/// </summary>
public sealed class BffSecurityOptionsValidator(IHostEnvironment environment) : IValidateOptions<BffSecurityOptions>
{
    /// <summary>
    /// Verifica caminhos, certificado, origens permitidas e proxies confiáveis.
    /// </summary>
    /// <param name="name">Nome da instância de opções validada.</param>
    /// <param name="options">Opções de segurança a validar.</param>
    /// <returns>Sucesso quando as opções são válidas; caso contrário, os erros de validação.</returns>
    public ValidateOptionsResult Validate(string? name, BffSecurityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DataProtectionKeysPath))
        {
            return ValidateOptionsResult.Fail("Security:DataProtectionKeysPath é obrigatório.");
        }

        if (!environment.IsDevelopment()
            && (string.IsNullOrWhiteSpace(options.DataProtectionCertificatePath)
                || string.IsNullOrWhiteSpace(options.DataProtectionCertificatePassword)))
        {
            return ValidateOptionsResult.Fail(
                "Certificado e senha para proteção do key ring são obrigatórios fora de Development.");
        }

        if (options.AllowedOrigins.Count == 0
            || options.AllowedOrigins.Any(origin => !IsValidOrigin(origin)))
        {
            return ValidateOptionsResult.Fail("Security:AllowedOrigins deve conter somente origens absolutas permitidas.");
        }

        if ((options.TrustedProxies.Count == 0 && options.TrustedNetworks.Count == 0)
            || options.TrustedProxies.Any(proxy => !IPAddress.TryParse(proxy, out _))
            || options.TrustedNetworks.Any(network => !System.Net.IPNetwork.TryParse(network, out _)))
        {
            return ValidateOptionsResult.Fail(
                "Security:TrustedProxies e Security:TrustedNetworks devem conter somente endereços IP ou redes CIDR explícitos.");
        }

        return ValidateOptionsResult.Success;
    }

    private bool IsValidOrigin(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var origin)
            || !string.IsNullOrEmpty(origin.Query)
            || !string.IsNullOrEmpty(origin.Fragment)
            || origin.AbsolutePath != "/")
        {
            return false;
        }

        return string.Equals(origin.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || (environment.IsDevelopment() && origin.IsLoopback);
    }
}
