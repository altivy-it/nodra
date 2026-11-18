using Microsoft.Extensions.Logging;
using Nodra.Backend.Application.Vaults;

namespace Nodra.Notes.Worker.Notifications;

/// <summary>
/// Registra alertas administrativos sem enviá-los quando SMTP não está configurado em Development.
/// </summary>
public sealed class DevelopmentAdministrativeNotifier(ILogger<DevelopmentAdministrativeNotifier> logger)
    : IAdministrativeNotifier
{
    /// <inheritdoc />
    public Task NotifyAsync(VaultResidueAlert alert, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogWarning(
            "Administrative alert suppressed in Development. Type={AlertType} OccurrenceId={OccurrenceId}",
            alert.Type,
            alert.OccurrenceId);
        return Task.CompletedTask;
    }
}
