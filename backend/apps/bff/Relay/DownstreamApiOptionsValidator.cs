using Altivy.Bff.Configuration;

namespace Altivy.Bff.Relay;

/// <summary>
/// Valida as configurações da API downstream antes da aplicação iniciar.
/// </summary>
public sealed class DownstreamApiOptionsValidator(
    IHostEnvironment environment,
    IOptions<BffRuntimeOptions> runtimeOptions)
    : IValidateOptions<DownstreamApiOptions>
{
    /// <summary>
    /// Verifica se a URL downstream é absoluta e usa transporte seguro fora do desenvolvimento local.
    /// </summary>
    /// <param name="name">Nome da instância de opções validada.</param>
    /// <param name="options">Opções da API downstream a validar.</param>
    /// <returns>Sucesso quando as opções são válidas; caso contrário, os erros de validação.</returns>
    public ValidateOptionsResult Validate(string? name, DownstreamApiOptions options)
    {
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUrl))
        {
            return ValidateOptionsResult.Fail("Downstream:Api:BaseUrl deve ser uma URL absoluta.");
        }

        if (!string.Equals(baseUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !(environment.IsDevelopment() && (baseUrl.IsLoopback || runtimeOptions.Value.AllowInsecureHttpForDevelopment)))
        {
            return ValidateOptionsResult.Fail("A API interna deve usar HTTPS, exceto em desenvolvimento local.");
        }

        return ValidateOptionsResult.Success;
    }
}
