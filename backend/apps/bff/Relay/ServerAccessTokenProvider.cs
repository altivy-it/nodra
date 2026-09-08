using Microsoft.AspNetCore.Authentication;

namespace Altivy.Bff.Relay;

/// <summary>
/// Obtém o token de acesso armazenado na sessão HTTP autenticada do BFF.
/// </summary>
public sealed class ServerAccessTokenProvider(IHttpContextAccessor httpContextAccessor)
    : IAccessTokenProvider
{
    /// <summary>
    /// Recupera o token de acesso da sessão HTTP atual.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Token de acesso armazenado na sessão.</returns>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Não existe contexto HTTP para obter o token de acesso.");
        var accessToken = await context.GetTokenAsync("access_token");

        return !string.IsNullOrWhiteSpace(accessToken)
            ? accessToken
            : throw new UnauthorizedAccessException("A sessão autenticada não possui token de acesso.");
    }
}
