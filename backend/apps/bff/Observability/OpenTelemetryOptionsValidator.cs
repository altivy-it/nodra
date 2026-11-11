using Microsoft.Extensions.Options;

namespace Nodra.Bff.Observability;

/// <summary>
/// Valida o endpoint OTLP conforme o ambiente de execução.
/// </summary>
public sealed class OpenTelemetryOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<OpenTelemetryOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, OpenTelemetryOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            return ValidateOptionsResult.Success;
        }

        if (!Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var endpoint)
            || endpoint.Query.Length > 0
            || endpoint.Fragment.Length > 0)
        {
            return ValidateOptionsResult.Fail("O endpoint OTLP deve ser uma URL absoluta sem query ou fragmento.");
        }

        if (!string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !(environment.IsDevelopment() && endpoint.IsLoopback))
        {
            return ValidateOptionsResult.Fail("O endpoint OTLP deve usar HTTPS fora do loopback de Development.");
        }

        return ValidateOptionsResult.Success;
    }
}