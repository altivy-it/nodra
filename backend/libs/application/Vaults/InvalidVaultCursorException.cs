namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Indica que um cursor de paginação de vault não pode ser interpretado.
/// </summary>
public sealed class InvalidVaultCursorException : Exception
{
    /// <summary>
    /// Inicializa a exceção para um cursor inválido.
    /// </summary>
    public InvalidVaultCursorException()
        : base("O cursor de vault é inválido.")
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem específica.
    /// </summary>
    /// <param name="message">Mensagem que descreve o erro.</param>
    public InvalidVaultCursorException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Inicializa a exceção para um cursor inválido preservando a causa técnica.
    /// </summary>
    /// <param name="innerException">Exceção que impediu a leitura do cursor.</param>
    public InvalidVaultCursorException(Exception innerException)
        : base("O cursor de vault é inválido.", innerException)
    {
    }

    /// <summary>
    /// Inicializa a exceção com uma mensagem específica e a causa técnica.
    /// </summary>
    /// <param name="message">Mensagem que descreve o erro.</param>
    /// <param name="innerException">Exceção que causou o erro atual.</param>
    public InvalidVaultCursorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
