namespace Altivy.Bff.Authentication;

/// <summary>
/// Define a chave de particionamento do limite de tentativas de login.
/// </summary>
public static class LoginRateLimitPartition
{
    /// <summary>
    /// Obtém o endereço IP remoto usado para limitar tentativas de login.
    /// </summary>
    /// <param name="context">Contexto HTTP da tentativa de login.</param>
    /// <returns>Chave textual baseada no endereço IP remoto.</returns>
    public static string GetKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}