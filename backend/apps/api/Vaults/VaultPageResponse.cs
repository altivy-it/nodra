namespace Altivy.Notes.Api.Vaults;

/// <summary>
/// Representa uma página de vaults autorizados.
/// </summary>
/// <param name="Items">Vaults autorizados da página.</param>
/// <param name="NextCursor">Cursor opaco para continuar a listagem.</param>
public sealed record VaultPageResponse(
    IReadOnlyList<VaultSummaryResponse> Items,
    string? NextCursor);
