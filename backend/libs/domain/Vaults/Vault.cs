namespace Altivy.Backend.Domain.Vaults;

/// <summary>
/// Representa um vault lógico do Altivy, independente de seu armazenamento físico.
/// </summary>
public sealed class Vault
{
    private Vault(string id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Obtém o identificador opaco e estável do vault.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Obtém o nome exibido para o vault.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Cria um vault depois de validar suas invariantes de domínio.
    /// </summary>
    /// <param name="id">Identificador em letras minúsculas, números e hífen.</param>
    /// <param name="name">Nome não vazio exibido ao usuário.</param>
    /// <returns>O vault criado.</returns>
    /// <exception cref="ArgumentException">Quando o identificador ou nome é inválido.</exception>
    public static Vault Create(string id, string name)
    {
        if (!IsValidId(id))
        {
            throw new ArgumentException("O identificador do vault é inválido.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            throw new ArgumentException("O nome do vault é inválido.", nameof(name));
        }

        return new Vault(id, name.Trim());
    }

    private static bool IsValidId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
        {
            return false;
        }

        return value.All(character =>
            character is >= 'a' and <= 'z' ||
            character is >= '0' and <= '9' ||
            character == '-');
    }
}
