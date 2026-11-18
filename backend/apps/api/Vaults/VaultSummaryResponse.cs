namespace Nodra.Notes.Api.Vaults;

/// <summary>
/// Representa um vault cuja leitura foi autorizada.
/// </summary>
/// <param name="Id">Identificador técnico e opaco do vault.</param>
/// <param name="Name">Nome de exibição do vault.</param>
public sealed record VaultSummaryResponse(string Id, string Name);
