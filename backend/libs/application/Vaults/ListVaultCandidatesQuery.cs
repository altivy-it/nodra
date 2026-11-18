namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Solicita uma página de candidatos a vault.
/// </summary>
/// <param name="Cursor">Cursor opaco da página anterior.</param>
/// <param name="Limit">Quantidade máxima de candidatos.</param>
public sealed record ListVaultCandidatesQuery(string? Cursor, int Limit);
