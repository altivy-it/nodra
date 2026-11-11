namespace Nodra.Notes.Api.Authorization;

public sealed class KeycloakAuthorizationOptions
{
    public required string Authority { get; init; }

    public required string Audience { get; init; }
}
