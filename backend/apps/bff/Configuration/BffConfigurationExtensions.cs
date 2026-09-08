using Altivy.Bff.Authentication;
using Altivy.Bff.Relay;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altivy.Bff.Configuration;

/// <summary>
/// Fornece métodos para registrar a configuração tipada do BFF.
/// </summary>
public static class BffConfigurationExtensions
{
    /// <summary>
    /// Vincula e valida as opções de autenticação, segurança e API downstream do BFF.
    /// </summary>
    /// <param name="builder">Builder da aplicação que receberá os registros de configuração.</param>
    /// <returns>O mesmo builder, permitindo encadear outras extensões de composição.</returns>
    public static WebApplicationBuilder AddBffConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .ValidateOnStart();
        builder.Services
            .AddOptions<BffSecurityOptions>()
            .BindConfiguration(BffSecurityOptions.SectionName)
            .ValidateOnStart();
        builder.Services
            .AddOptions<DownstreamApiOptions>()
            .BindConfiguration(DownstreamApiOptions.SectionName)
            .ValidateOnStart();
        builder.Services
            .AddOptions<BffRuntimeOptions>()
            .BindConfiguration(BffRuntimeOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<KeycloakOptions>, KeycloakOptionsValidator>();
        builder.Services.AddSingleton<IValidateOptions<BffSecurityOptions>, BffSecurityOptionsValidator>();
        builder.Services.AddSingleton<IValidateOptions<DownstreamApiOptions>, DownstreamApiOptionsValidator>();

        return builder;
    }
}