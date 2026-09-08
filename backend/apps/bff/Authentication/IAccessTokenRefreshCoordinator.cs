namespace Altivy.Bff.Authentication;

/// <summary>
/// Define o contrato para renovação coordenada de tokens de acesso.
/// </summary>
public interface IAccessTokenRefreshCoordinator
{
    /// <summary>
    /// Renova os tokens associados ao refresh token informado.
    /// </summary>
    /// <param name="refreshToken">Refresh token emitido pelo Keycloak.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Resultado contendo os tokens renovados e sua expiração.</returns>
    Task<AccessTokenRefreshResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}