using System.Security.Claims;

namespace Nodra.Bff.Endpoints;

/// <summary>
/// Representa o estado público e sanitizado da sessão do browser.
/// </summary>
/// <param name="Authenticated">Indica se existe uma sessão autenticada.</param>
/// <param name="Subject">Identificador técnico do sujeito, quando autenticado.</param>
/// <param name="Name">Nome público da identidade, quando disponível.</param>
public sealed record BffSessionResponse(bool Authenticated, string? Subject, string? Name)
{
    /// <summary>
    /// Cria a resposta pública a partir do principal HTTP atual.
    /// </summary>
    /// <param name="user">Principal associado à requisição.</param>
    /// <returns>Projeção sem tokens ou claims sensíveis.</returns>
    public static BffSessionResponse FromPrincipal(ClaimsPrincipal user)
    {
        var authenticated = user.Identity?.IsAuthenticated == true;
        return new BffSessionResponse(
            authenticated,
            authenticated ? user.FindFirstValue("sub") : null,
            authenticated ? user.Identity?.Name : null);
    }
}
