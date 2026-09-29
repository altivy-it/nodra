namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Representa um filho direto de uma pasta do vault.
/// </summary>
/// <param name="Path">Caminho relativo normalizado da entrada.</param>
/// <param name="Name">Nome exibido da entrada.</param>
/// <param name="IsFolder">Indica se a entrada é uma pasta.</param>
/// <param name="ETag">Versão da nota, ou <see langword="null"/> para pastas.</param>
/// <param name="IsUnclassified">Indica ausência de classificação válida; nulo para pastas.</param>
/// <param name="HasMinimumMetadata">Indica presença dos metadados mínimos; nulo para pastas.</param>
/// <param name="HasUnresolvedLinks">Indica a presença de links internos não resolvidos; nulo para pastas.</param>
public sealed record VaultEntry(
    string Path,
    string Name,
    bool IsFolder,
    string? ETag,
    bool? IsUnclassified = null,
    bool? HasMinimumMetadata = null,
    bool? HasUnresolvedLinks = null);
