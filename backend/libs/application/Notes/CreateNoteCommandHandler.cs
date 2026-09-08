using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Coordena a criação de uma nova nota no vault autorizado pela camada de entrada.
/// </summary>
public sealed class CreateNoteCommandHandler(INoteStore noteStore)
{
    /// <summary>
    /// Cria a nota quando o caminho ainda não está em uso.
    /// </summary>
    /// <param name="command">Dados da nova nota.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns><see langword="true"/> quando criada; caso contrário, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Quando os dados da nota são inválidos.</exception>
    public Task<bool> HandleAsync(CreateNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.VaultId) || command.VaultId.Length > 64)
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(command));
        }

        if (command.Content.Length > 1_048_576)
        {
            throw new ArgumentException("O conteúdo da nota excede o limite permitido.", nameof(command));
        }

        return noteStore.TryCreateAsync(
            command.VaultId,
            NotePath.Create(command.Path),
            command.Content,
            cancellationToken);
    }
}