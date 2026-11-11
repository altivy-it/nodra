namespace Nodra.Bff.Authentication;

/// <summary>
/// Representa o resultado da renovação de tokens realizada pelo BFF.
/// </summary>
public sealed record AccessTokenRefreshResult(
    /// <summary>
    /// Token de acesso renovado.
    /// </summary>
    string AccessToken,
    /// <summary>
    /// Refresh token válido para novas renovações.
    /// </summary>
    string RefreshToken,
    /// <summary>
    /// Data e hora UTC em que o token de acesso expira.
    /// </summary>
    DateTimeOffset ExpiresAt);
