namespace Nodra.Bff.Authentication;

/// <summary>
/// Representa a rejeição não transitória de uma renovação de token pelo Keycloak.
/// </summary>
public sealed class TokenRefreshRejectedException : InvalidOperationException
{
    /// <summary>
    /// Cria a exceção a partir do status HTTP retornado pelo Keycloak.
    /// </summary>
    /// <param name="statusCode">Status HTTP retornado pela renovação rejeitada.</param>
    public TokenRefreshRejectedException(int statusCode)
        : base($"O Keycloak recusou a renovação do token com HTTP {statusCode}.")
    {
    }

    /// <summary>
    /// Cria a exceção sem detalhes adicionais, exigido pelas convenções padrão de exceções .NET.
    /// </summary>
    public TokenRefreshRejectedException()
    {
    }

    /// <summary>
    /// Cria a exceção com uma mensagem customizada.
    /// </summary>
    /// <param name="message">Mensagem descritiva do erro.</param>
    public TokenRefreshRejectedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Cria a exceção com uma mensagem customizada e a exceção interna correspondente.
    /// </summary>
    /// <param name="message">Mensagem descritiva do erro.</param>
    /// <param name="innerException">Exceção original que causou a falha.</param>
    public TokenRefreshRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}