using Nodra.Backend.Domain.Vaults;

namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Inicia o provisioning idempotente de um vault pessoal.
/// </summary>
public sealed class CreateVaultCommandHandler(
    IVaultProvisioningStore provisioningStore,
    IVaultIdGenerator vaultIdGenerator,
    Func<Guid> operationIdFactory)
{
    /// <summary>
    /// Cria ou reutiliza a operação associada à chave de idempotência.
    /// </summary>
    /// <param name="command">Comando validado na borda da aplicação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificadores e estado inicial da operação.</returns>
    public async Task<CreateVaultResult> HandleAsync(
        CreateVaultCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);
        var existing = await provisioningStore.FindByIdempotencyKeyAsync(
            command.Subject.Trim(),
            command.IdempotencyKey.Trim(),
            cancellationToken);

        if (existing is not null)
        {
            return new CreateVaultResult(existing.OperationId, existing.VaultId, existing.State, true);
        }

        var operation = VaultProvisioningOperation.Create(
            operationIdFactory(),
            vaultIdGenerator.Generate(),
            command.Name,
            command.Subject);
        await provisioningStore.AddAsync(operation, command.IdempotencyKey.Trim(), cancellationToken);

        return new CreateVaultResult(operation.OperationId, operation.VaultId, operation.State, false);
    }

    private static void Validate(CreateVaultCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Subject)) throw new ArgumentException("O sujeito é obrigatório.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 128)
        {
            throw new ArgumentException("A chave de idempotência é inválida.", nameof(command));
        }
    }
}