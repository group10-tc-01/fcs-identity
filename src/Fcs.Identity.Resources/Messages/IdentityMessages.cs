using System.Diagnostics.CodeAnalysis;

namespace Fcs.Identity.Resources.Messages;

[ExcludeFromCodeCoverage]
public static class IdentityMessages
{
    public const string CpfInvalid = "CPF is invalid.";
    public const string CpfRequired = "CPF is required.";
    public const string CpfAlreadyExists = "A donor profile with this CPF already exists.";
    public const string CreatedUserNotFoundInIdentityProvider = "Could not find the created user in the identity provider.";
    public const string DonorProfileEmailAlreadyExists = "A donor profile with this email already exists.";
    public const string ManagerPasswordResetFailed = "Could not reset manager password in the identity provider.";
    public const string RealmRoleAssignmentFailed = "Could not assign the realm role in the identity provider.";
    public const string RealmRoleResolutionFailed = "Could not resolve the realm role in the identity provider.";
    public const string EmailInvalid = "Email is invalid.";
    public const string EmailRequired = "Email is required.";
    public const string FullNameRequired = "Full name is required.";
    public const string IdentityProviderAccessTokenMissing = "The identity provider did not return an access token.";
    public const string IdentityProviderAdminAccessTokenMissing = "The identity provider did not return an admin access token.";
    public const string IdentityProviderAdminAuthenticationFailed = "Could not authenticate with the identity provider admin API.";
    public const string IdentityProviderAuthenticationFailed = "Could not authenticate with the identity provider.";
    public const string IdentityProviderCreateDonorFailed = "Could not create donor user in the identity provider.";
    public const string IdentityProviderCreateManagerFailed = "Could not create manager user in the identity provider.";
    public const string IdentityProviderRefreshTokenFailed = "Could not refresh token with the identity provider.";
    public const string IdentityProviderRequestTimedOut = "The identity provider request timed out.";
    public const string IdentityProviderUnavailable = "The identity provider is unavailable.";
    public const string InvalidCredentials = "Invalid email or password.";
    public const string InvalidRefreshToken = "Invalid refresh token.";
    public const string KeycloakUserIdRequired = "Keycloak user id is required.";
    public const string PasswordMinimumLength = "Password must have at least 8 characters.";
    public const string PasswordRequired = "Password is required.";
    public const string ProfileNotFound = "Profile was not found.";
    public const string RefreshTokenRequired = "Refresh token is required.";
    public const string RoleNotFoundInIdentityProvider = "The role '{0}' was not found in the identity provider.";
    public const string RoleNotAllowed = "User role is not allowed.";
    public const string SingleUserResolutionFailedInIdentityProvider = "Could not resolve a single user in the identity provider.";
    public const string UserAlreadyExists = "A user with this email already exists.";
    public const string UserAlreadyExistsInIdentityProvider = "A user with this email already exists in the identity provider.";
    public const string UserNotFoundInIdentityProvider = "Could not find the user in the identity provider.";
    public const string UserNotAuthenticated = "User is not authenticated.";
}
