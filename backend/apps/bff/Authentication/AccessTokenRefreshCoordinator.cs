using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Nodra.Bff.Authentication;

/// <summary>
/// Coordena a renovação de tokens de acesso no Keycloak, evitando chamadas duplicadas concorrentes.
/// </summary>
public sealed class AccessTokenRefreshCoordinator(
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakOptions> keycloakOptions,
    IMemoryCache cache,
    TimeProvider timeProvider) : IAccessTokenRefreshCoordinator
{
    /// <summary>
    /// Nome do cliente HTTP usado para chamadas de renovação ao Keycloak.
    /// </summary>
    public const string HttpClientName = "KeycloakTokenRefresh";
    private static readonly TimeSpan s_resultLifetime = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, Lazy<Task<AccessTokenRefreshResult>>> inFlight = new();

    /// <summary>
    /// Renova um token de acesso e reutiliza resultados recentes para o mesmo refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token emitido pelo Keycloak.</param>
    /// <param name="cancellationToken">Token para cancelar a espera pelo resultado da renovação.</param>
    /// <returns>Tokens renovados e o horário de expiração do token de acesso.</returns>
    public async Task<AccessTokenRefreshResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        var key = GetCacheKey(refreshToken);
        if (cache.TryGetValue(key, out AccessTokenRefreshResult? cached) && cached is not null)
        {
            return cached;
        }

        var refresh = inFlight.GetOrAdd(
            key,
            _ => new Lazy<Task<AccessTokenRefreshResult>>(
                () => RefreshCoreAsync(refreshToken),
                LazyThreadSafetyMode.ExecutionAndPublication));
        _ = refresh.Value.ContinueWith(
            _ => inFlight.TryRemove(KeyValuePair.Create(key, refresh)),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return await refresh.Value.WaitAsync(cancellationToken);
    }

    private async Task<AccessTokenRefreshResult> RefreshCoreAsync(string refreshToken)
    {
        var keycloak = keycloakOptions.Value;
        var endpoint = new Uri(
            $"{keycloak.Authority.TrimEnd('/')}/protocol/openid-connect/token",
            UriKind.Absolute);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = keycloak.ClientId,
                ["client_secret"] = keycloak.ClientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            })
        };
        using var response = await httpClientFactory
            .CreateClient(HttpClientName)
            .SendAsync(request, CancellationToken.None);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            if (statusCode is >= 400 and < 500 and not 408 and not 429)
            {
                throw new TokenRefreshRejectedException(statusCode);
            }

            throw new HttpRequestException(
                $"A renovação do token falhou com HTTP {statusCode}.",
                null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(CancellationToken.None)
            ?? throw new InvalidOperationException("O Keycloak retornou uma resposta de renovação vazia.");
        if (string.IsNullOrWhiteSpace(payload.AccessToken) || payload.ExpiresIn <= 0)
        {
            throw new InvalidOperationException("O Keycloak retornou uma resposta de renovação inválida.");
        }

        var result = new AccessTokenRefreshResult(
            payload.AccessToken,
            string.IsNullOrWhiteSpace(payload.RefreshToken) ? refreshToken : payload.RefreshToken,
            timeProvider.GetUtcNow().AddSeconds(payload.ExpiresIn));
        _ = cache.Set(GetCacheKey(refreshToken), result, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = result.ExpiresAt < timeProvider.GetUtcNow().Add(s_resultLifetime)
                ? result.ExpiresAt
                : timeProvider.GetUtcNow().Add(s_resultLifetime),
            Size = 1
        });

        return result;
    }

    private static string GetCacheKey(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        /// <summary>
        /// Token de acesso retornado pelo Keycloak.
        /// </summary>
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        /// <summary>
        /// Refresh token retornado pelo Keycloak.
        /// </summary>
        public string RefreshToken { get; init; } = string.Empty;

        [JsonPropertyName("expires_in")]
        /// <summary>
        /// Tempo de validade do token de acesso em segundos.
        /// </summary>
        public int ExpiresIn { get; init; }
    }
}
