using Nodra.Backend.Application.Vaults;

namespace Nodra.Notes.Api.Endpoints;

/// <summary>
/// Produz a recusa de escrita em vaults somente leitura antes de consultar o Keycloak.
/// </summary>
internal static class ReadOnlyVaultProblem
{
    /// <summary>
    /// Retorna o problema <c>vault-read-only</c> quando o scope solicitado altera um vault somente leitura.
    /// </summary>
    /// <param name="vaultId">Identificador validado do vault.</param>
    /// <param name="scope">Scope exigido pela operação.</param>
    /// <returns>O resultado de recusa ou <see langword="null"/> quando a operação pode seguir para autorização.</returns>
    internal static IResult? For(string vaultId, string scope) =>
        SampleVault.IsReadOnly(vaultId) && scope != VaultAuthorization.ReadScope
            ? Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Vault de exemplo é somente leitura.",
                extensions: new Dictionary<string, object?> { ["code"] = SampleVault.ReadOnlyProblemCode })
            : null;
}
