using Altivy.Backend.Application.Vaults;

namespace Altivy.Notes.Api.Vaults;

/// <summary>
/// Representa os dados mínimos de um vault após autorização de leitura.
/// </summary>
/// <param name="Id">Identificador opaco do vault.</param>
/// <param name="Scope">Scope concedido pelo Keycloak.</param>
public sealed record VaultAccessResponse(string Id, string Scope);
