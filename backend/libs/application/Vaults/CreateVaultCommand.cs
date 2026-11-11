namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Solicita a criação idempotente de um vault pessoal.
/// </summary>
/// <param name="Name">Nome exibido do vault.</param>
/// <param name="Subject">Sujeito autenticado proprietário do vault.</param>
/// <param name="IdempotencyKey">Chave fornecida pelo cliente para retries.</param>
public sealed record CreateVaultCommand(string Name, string Subject, string IdempotencyKey);