namespace Altivy.Notes.Worker.Notifications;

/// <summary>
/// Configuração SMTP usada para alertas administrativos de resíduos.
/// </summary>
public sealed class AdministrativeSmtpOptions
{
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "ResidueAlerts:Smtp";

    /// <summary>Obtém ou define o host SMTP.</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>Obtém ou define a porta SMTP.</summary>
    public int Port { get; init; } = 587;

    /// <summary>Obtém ou define o usuário SMTP fornecido somente em runtime.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>Obtém ou define a senha SMTP fornecida somente em runtime.</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Obtém ou define o endereço remetente.</summary>
    public string From { get; init; } = string.Empty;

    /// <summary>Obtém ou define a caixa operacional do administrador.</summary>
    public string To { get; init; } = string.Empty;
}