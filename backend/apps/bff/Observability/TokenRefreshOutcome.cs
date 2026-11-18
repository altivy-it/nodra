namespace Nodra.Bff.Observability;

/// <summary>
/// Classifica resultados possíveis da renovação de sessão com cardinalidade fixa.
/// </summary>
public enum TokenRefreshOutcome
{
    /// <summary>Renovação concluída com sucesso.</summary>
    Success,

    /// <summary>Renovação rejeitada permanentemente pelo IdP.</summary>
    Rejected,

    /// <summary>Renovação interrompida por falha transitória.</summary>
    TransientFailure,

    /// <summary>Sessão sem refresh token.</summary>
    RefreshTokenMissing
}
