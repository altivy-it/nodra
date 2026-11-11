namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Indica uma falha transitória ao entregar uma notificação administrativa.
/// </summary>
public sealed class AdministrativeNotificationDeliveryException : Exception
{
    /// <summary>Inicializa a exceção sem propagar detalhes do canal de entrega.</summary>
    public AdministrativeNotificationDeliveryException()
        : base("Não foi possível entregar a notificação administrativa.")
    {
    }

    /// <summary>Inicializa a exceção com uma mensagem sanitizada.</summary>
    /// <param name="message">Mensagem sanitizada.</param>
    public AdministrativeNotificationDeliveryException(string message)
        : base(message)
    {
    }

    /// <summary>Inicializa a exceção com uma mensagem sanitizada e causa interna.</summary>
    /// <param name="message">Mensagem sanitizada.</param>
    /// <param name="innerException">Causa interna a ser tratada pelo chamador.</param>
    public AdministrativeNotificationDeliveryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}