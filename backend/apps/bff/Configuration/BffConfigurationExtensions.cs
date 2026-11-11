using Nodra.Bff.Authentication;
using Nodra.Bff.Relay;

namespace Nodra.Bff.Configuration;

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
        _ = builder.Services
            .AddOptions<KeycloakOptions>()
            .BindConfiguration(KeycloakOptions.SectionName)
            .ValidateOnStart();
        _ = builder.Services
            .AddOptions<BffSecurityOptions>()
            .BindConfiguration(BffSecurityOptions.SectionName)
            .ValidateOnStart();
        _ = builder.Services
            .AddOptions<DownstreamApiOptions>()
            .BindConfiguration(DownstreamApiOptions.SectionName)
            .ValidateOnStart();
        _ = builder.Services
            .AddOptions<BffRuntimeOptions>()
            .BindConfiguration(BffRuntimeOptions.SectionName)
            .ValidateOnStart();
        _ = builder.Services.AddSingleton<IValidateOptions<KeycloakOptions>, KeycloakOptionsValidator>();
        _ = builder.Services.AddSingleton<IValidateOptions<BffSecurityOptions>, BffSecurityOptionsValidator>();
        _ = builder.Services.AddSingleton<IValidateOptions<DownstreamApiOptions>, DownstreamApiOptionsValidator>();

        return builder;
    }
}
