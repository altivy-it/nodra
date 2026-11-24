namespace Nodra.Backend.Application.Vaults;

/// <summary>
/// Identifica o vault de sistema de exemplo, cujo conteúdo é fornecido pela aplicação e nunca é alterado.
/// </summary>
public static class SampleVault
{
    /// <summary>
    /// Identificador estável do vault de exemplo.
    /// </summary>
    public const string Id = "nodra-exemplo";

    /// <summary>
    /// Nome de exibição do vault de exemplo.
    /// </summary>
    public const string Name = "Vault de exemplo";

    /// <summary>
    /// Caminho relativo do documento modelo dentro do vault de exemplo.
    /// </summary>
    public const string DocumentPath = "documento-modelo.md";

    /// <summary>
    /// Código do problema retornado quando uma escrita é solicitada em um vault somente leitura.
    /// </summary>
    public const string ReadOnlyProblemCode = "vault-read-only";

    /// <summary>
    /// Indica se o vault informado recusa qualquer escrita, independentemente das permissões concedidas.
    /// </summary>
    /// <param name="vaultId">Identificador do vault.</param>
    /// <returns><see langword="true"/> quando o vault é somente leitura.</returns>
    public static bool IsReadOnly(string vaultId) => string.Equals(vaultId, Id, StringComparison.Ordinal);
}
