namespace Fcg.Identity.Application.Abstractions.Identity;

public sealed record LoginIdentityUserResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    string? KeycloakUserId = null,
    IReadOnlyCollection<string>? Roles = null);
