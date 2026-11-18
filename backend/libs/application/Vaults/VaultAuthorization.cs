namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Define os recursos e scopes usados para autorização contextual de vaults.
/// </summary>
public static class VaultAuthorization
{
    /// <summary>
    /// Scope de leitura de metadados e conteúdo autorizado do vault.
    /// </summary>
    public const string ReadScope = "vault:read";

    /// <summary>
    /// Scope de escrita de conteúdo autorizado do vault.
    /// </summary>
    public const string WriteScope = "vault:write";

    /// <summary>
    /// Scope de administração do vault.
    /// </summary>
    public const string ManageScope = "vault:manage";

    /// <summary>
    /// Scope de exclusão do vault.
    /// </summary>
    public const string DeleteScope = "vault:delete";

    /// <summary>
    /// Cria o identificador do recurso correspondente a um vault.
    /// </summary>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <returns>Identificador técnico usado pelo Keycloak.</returns>
    public static string ResourceFor(string vaultId) => $"vault-{vaultId}";
}
