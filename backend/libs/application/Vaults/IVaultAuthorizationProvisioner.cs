namespace Altivy.Backend.Application.Vaults;

/// <summary>
/// Provisiona a autorização externa necessária para o proprietário de um vault.
/// </summary>
public interface IVaultAuthorizationProvisioner
{
    /// <summary>
    /// Garante que o proprietário possua as permissions de leitura e escrita do vault.
    /// </summary>
    /// <param name="vaultId">Identificador opaco gerado pelo servidor.</param>
    /// <param name="subject">Sujeito externo proprietário do vault.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Uma tarefa que representa o provisionamento da autorização.</returns>
    Task ProvisionOwnerAccessAsync(string vaultId, string subject, CancellationToken cancellationToken);
}