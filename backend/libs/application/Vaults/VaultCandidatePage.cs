namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Representa uma página ordenada de candidatos a vault.
/// </summary>
/// <param name="Items">Candidatos da página.</param>
/// <param name="NextCursor">Cursor opaco para a próxima página, quando houver.</param>
public sealed record VaultCandidatePage(
    IReadOnlyList<VaultCandidate> Items,
    string? NextCursor);