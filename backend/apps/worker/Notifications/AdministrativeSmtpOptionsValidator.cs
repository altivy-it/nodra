using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Altivy.Notes.Worker.Notifications;

/// <summary>
/// Valida a configuração SMTP antes de iniciar o worker.
/// </summary>
public sealed class AdministrativeSmtpOptionsValidator : IValidateOptions<AdministrativeSmtpOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AdministrativeSmtpOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host) || options.Port is < 1 or > 65535)
        {
            return ValidateOptionsResult.Fail("ResidueAlerts:Smtp requer Host e Port válidos.");
        }

        if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Password))
        {
            return ValidateOptionsResult.Fail("ResidueAlerts:Smtp requer Username e Password fornecidos em runtime.");
        }

        try
        {
            _ = new MailAddress(options.From);
            _ = new MailAddress(options.To);
        }
        catch (FormatException)
        {
            return ValidateOptionsResult.Fail("ResidueAlerts:Smtp requer From e To com endereços de e-mail válidos.");
        }

        return ValidateOptionsResult.Success;
    }
}