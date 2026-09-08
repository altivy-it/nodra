namespace Altivy.Backend.Application.Vaults;

/// <summary>
/// Gera identificadores opacos de vault no servidor.
/// </summary>
public interface IVaultIdGenerator
{
    /// <summary>Gera um identificador válido e único.</summary>
    /// <returns>Identificador do vault.</returns>
    string Generate();
}