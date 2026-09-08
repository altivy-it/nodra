namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Representa a solicitação de criação de uma nota Markdown.
/// </summary>
/// <param name="VaultId">Identificador estável do vault.</param>
/// <param name="Path">Caminho relativo da nota.</param>
/// <param name="Content">Conteúdo Markdown inicial.</param>
public sealed record CreateNoteCommand(string VaultId, string Path, string Content);
