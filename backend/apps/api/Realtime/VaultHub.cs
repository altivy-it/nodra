using Altivy.Backend.Application.Vaults;
using Altivy.Notes.Api.Authorization;
using Altivy.Notes.Api.Vaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Altivy.Notes.Api.Realtime;

/// <summary>
/// Hub SignalR que publica atualizações de um vault autorizado pelo Keycloak.
/// </summary>
[Authorize]
public sealed class VaultHub(IAuthorizationDecisionService authorization) : Hub
{
    /// <summary>
    /// Consulta a decisão de autorização do vault e adiciona a conexão ao grupo correspondente.
    /// </summary>
    /// <param name="vaultId">Identificador opaco do vault.</param>
    /// <exception cref="HubException">Quando o identificador é inválido ou o acesso é negado.</exception>
    public async Task JoinVaultAsync(string vaultId)
    {
        if (!VaultId.TryParse(vaultId, out var parsedVaultId))
        {
            throw new HubException("Identificador de vault inválido.");
        }

        var accessToken = ExtractAccessToken(Context.GetHttpContext());
        if (accessToken is null)
        {
            throw new HubException("Acesso negado.");
        }

        var decision = await authorization.EvaluateAsync(
            accessToken,
            VaultAuthorization.ResourceFor(parsedVaultId.Value),
            VaultAuthorization.ReadScope,
            Context.ConnectionAborted);

        if (!decision.Available || !decision.Allowed)
        {
            throw new HubException("Acesso negado.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(parsedVaultId), Context.ConnectionAborted);
    }

    /// <summary>
    /// Nome do grupo SignalR usado para publicar eventos de um vault específico.
    /// </summary>
    /// <param name="vaultId">Identificador opaco do vault.</param>
    /// <returns>Nome estável do grupo.</returns>
    public static string GroupFor(VaultId vaultId) => $"vault-{vaultId.Value}";

    private static string? ExtractAccessToken(HttpContext? httpContext)
    {
        var header = httpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header["Bearer ".Length..].Trim();
        return token.Length == 0 ? null : token;
    }
}
