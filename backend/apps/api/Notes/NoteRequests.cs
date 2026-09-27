namespace Altivy.Notes.Api.Notes;

/// <summary>
/// Representa o corpo recebido para criar ou atualizar uma nota.
/// </summary>
/// <param name="Content">Conteúdo Markdown canônico da nota.</param>
public sealed record NoteRequest(string Content);

/// <summary>
/// Representa o novo caminho relativo de uma nota a ser movida ou renomeada.
/// </summary>
/// <param name="Path">Caminho relativo de destino da nota Markdown.</param>
public sealed record NoteMoveRequest(string Path);

/// <summary>
/// Representa o novo caminho relativo de uma pasta a ser movida ou renomeada.
/// </summary>
/// <param name="Path">Caminho relativo de destino da pasta.</param>
public sealed record FolderMoveRequest(string Path);
