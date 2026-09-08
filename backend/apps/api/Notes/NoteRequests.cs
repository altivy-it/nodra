namespace Altivy.Notes.Api.Notes;

/// <summary>
/// Representa o corpo recebido para criar ou atualizar uma nota.
/// </summary>
/// <param name="Content">Conteúdo Markdown canônico da nota.</param>
public sealed record NoteRequest(string Content);
