namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Envia alertas sanitizados ao canal administrativo configurado.
/// </summary>
public interface IAdministrativeNotifier
{
    /// <summary>Envia um alerta ao administrador.</summary>
    /// <param name="alert">Alerta sanitizado a enviar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task NotifyAsync(VaultResidueAlert alert, CancellationToken cancellationToken);
}
