using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Representa uma nota canônica lida do vault.
/// </summary>
/// <param name="Path">Caminho relativo da nota.</param>
/// <param name="Content">Conteúdo Markdown da nota.</param>
/// <param name="ETag">Versão opaca do conteúdo para concorrência otimista.</param>
public sealed record NoteDocument(NotePath Path, string Content, string ETag);
