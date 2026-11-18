namespace Nodra.Bff.Relay;

/// <summary>
/// Define o contrato para obtenção do token de acesso da sessão atual.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Obtém o token de acesso usado para chamar a API downstream.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a obtenção do token.</param>
    /// <returns>Token de acesso Bearer válido.</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}
