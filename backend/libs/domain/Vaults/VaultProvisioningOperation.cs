namespace Altivy.Backend.Domain.Vaults;

/// <summary>
/// Controla o lifecycle idempotente do provisioning de um vault.
/// </summary>
public sealed class VaultProvisioningOperation
{
    private VaultProvisioningOperation(
        Guid operationId,
        string vaultId,
        string name,
        string subject,
        VaultLifecycleState state)
    {
        OperationId = operationId;
        VaultId = vaultId;
        Name = name;
        Subject = subject;
        State = state;
    }

    /// <summary>Obtém o identificador da operação.</summary>
    public Guid OperationId { get; }

    /// <summary>Obtém o identificador do vault provisionado.</summary>
    public string VaultId { get; }

    /// <summary>Obtém o nome do vault.</summary>
    public string Name { get; }

    /// <summary>Obtém o sujeito que iniciou a operação.</summary>
    public string Subject { get; }

    /// <summary>Obtém o estado atual do provisioning.</summary>
    public VaultLifecycleState State { get; private set; }

    /// <summary>
    /// Cria uma operação nova no estado pendente.
    /// </summary>
    /// <param name="operationId">Identificador único da operação.</param>
    /// <param name="vaultId">Identificador estável do vault.</param>
    /// <param name="name">Nome exibido do vault.</param>
    /// <param name="subject">Sujeito autenticado proprietário da operação.</param>
    /// <returns>A operação criada.</returns>
    public static VaultProvisioningOperation Create(
        Guid operationId,
        string vaultId,
        string name,
        string subject)
    {
        _ = Vault.Create(vaultId, name);
        if (operationId == Guid.Empty) throw new ArgumentException("O ID da operação é obrigatório.", nameof(operationId));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("O sujeito é obrigatório.", nameof(subject));

        return new VaultProvisioningOperation(operationId, vaultId, name.Trim(), subject.Trim(), VaultLifecycleState.Pending);
    }

    /// <summary>
    /// Avança a operação para um estado permitido.
    /// </summary>
    /// <param name="nextState">Novo estado desejado.</param>
    /// <exception cref="InvalidOperationException">Quando a transição não é permitida.</exception>
    public void TransitionTo(VaultLifecycleState nextState)
    {
        if (!IsAllowedTransition(State, nextState))
        {
            throw new InvalidOperationException($"A transição de {State} para {nextState} não é permitida.");
        }

        State = nextState;
    }

    private static bool IsAllowedTransition(VaultLifecycleState current, VaultLifecycleState next) =>
        current switch
        {
            VaultLifecycleState.Pending => next is VaultLifecycleState.Provisioning or VaultLifecycleState.Failed,
            VaultLifecycleState.Provisioning => next is VaultLifecycleState.Ready or VaultLifecycleState.Degraded or VaultLifecycleState.Failed,
            VaultLifecycleState.Ready => next is VaultLifecycleState.Degraded or VaultLifecycleState.Archived,
            VaultLifecycleState.Degraded => next is VaultLifecycleState.Provisioning or VaultLifecycleState.Failed or VaultLifecycleState.Archived,
            _ => false
        };
}