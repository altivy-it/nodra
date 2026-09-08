namespace Altivy.Backend.Application.Vaults;

/// <summary>
/// Representa metadados de um vault candidato, ainda sem decisão de autorização.
/// </summary>
/// <param name="Id">Identificador estável do vault.</param>
/// <param name="Name">Nome de exibição do vault.</param>
public sealed record VaultCandidate(string Id, string Name);
