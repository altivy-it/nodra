using System.Globalization;
using System.Net;
using System.Net.Mail;
using Nodra.Backend.Application.Vaults;
using Microsoft.Extensions.Options;

namespace Nodra.Notes.Worker.Notifications;

/// <summary>
/// Envia alertas operacionais sanitizados à caixa SMTP administrativa.
/// </summary>
public sealed class AdministrativeSmtpNotifier(IOptions<AdministrativeSmtpOptions> options) : IAdministrativeNotifier
{
    /// <inheritdoc />
    public async Task NotifyAsync(VaultResidueAlert alert, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuration = options.Value;
        using var client = new SmtpClient(configuration.Host, configuration.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(configuration.Username, configuration.Password)
        };
        using var message = new MailMessage(configuration.From, configuration.To)
        {
            Subject = $"[Altivy] Resíduo de vault: {alert.Type}",
            Body = string.Join(Environment.NewLine,
            [
                "Uma ocorrência de resíduo de vault requer investigação administrativa.",
                $"Ocorrência: {alert.OccurrenceId}",
                $"Vault: {alert.VaultId}",
                $"Tipo: {alert.Type}",
                $"Primeira detecção (UTC): {alert.FirstDetectedAt.ToString("O", CultureInfo.InvariantCulture)}",
                $"Última detecção (UTC): {alert.LastDetectedAt.ToString("O", CultureInfo.InvariantCulture)}",
                $"Tentativa de alerta: {alert.AttemptCount}",
                "Ação requerida: investigar o resíduo e decidir recuperação, retenção ou descarte conforme a política aprovada."
            ])
        };
        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException)
        {
            throw new AdministrativeNotificationDeliveryException();
        }
    }
}