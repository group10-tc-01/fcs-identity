using System.Diagnostics.CodeAnalysis;

namespace Fcs.Identity.Resources.Messages;

[ExcludeFromCodeCoverage]
public static class IdentityErrorCodes
{
    public const string CpfInvalid = "Cpf.Invalid";
    public const string CpfRequired = "Cpf.Required";
    public const string CurrentUserRoleNotAllowed = "CurrentUser.RoleNotAllowed";
    public const string CurrentUserUnauthenticated = "CurrentUser.Unauthenticated";
    public const string DonorProfileCpfAlreadyExists = "DonorProfile.CpfAlreadyExists";
    public const string DonorProfileCpfRequired = "DonorProfile.CpfRequired";
    public const string DonorProfileEmailAlreadyExists = "DonorProfile.EmailAlreadyExists";
    public const string DonorProfileEmailRequired = "DonorProfile.EmailRequired";
    public const string DonorProfileFullNameRequired = "DonorProfile.FullNameRequired";
    public const string DonorProfileKeycloakUserIdRequired = "DonorProfile.KeycloakUserIdRequired";
    public const string EmailInvalid = "Email.Invalid";
    public const string EmailRequired = "Email.Required";
    public const string IdentityProviderAdminAuthenticationFailed = "IdentityProvider.AdminAuthenticationFailed";
    public const string IdentityProviderAdminTokenMissing = "IdentityProvider.AdminTokenMissing";
    public const string IdentityProviderAssignRoleFailed = "IdentityProvider.AssignRoleFailed";
    public const string IdentityProviderCreateUserFailed = "IdentityProvider.CreateUserFailed";
    public const string IdentityProviderGetRoleFailed = "IdentityProvider.GetRoleFailed";
    public const string IdentityProviderInvalidCredentials = "IdentityProvider.InvalidCredentials";
    public const string IdentityProviderInvalidRefreshToken = "IdentityProvider.InvalidRefreshToken";
    public const string IdentityProviderLoginFailed = "IdentityProvider.LoginFailed";
    public const string IdentityProviderRefreshTokenFailed = "IdentityProvider.RefreshTokenFailed";
    public const string IdentityProviderResetPasswordFailed = "IdentityProvider.ResetPasswordFailed";
    public const string IdentityProviderRoleNotFound = "IdentityProvider.RoleNotFound";
    public const string IdentityProviderTimeout = "IdentityProvider.Timeout";
    public const string IdentityProviderTokenMissing = "IdentityProvider.TokenMissing";
    public const string IdentityProviderUnavailable = "IdentityProvider.Unavailable";
    public const string IdentityProviderUserAlreadyExists = "IdentityProvider.UserAlreadyExists";
    public const string IdentityProviderUserLookupAmbiguous = "IdentityProvider.UserLookupAmbiguous";
    public const string IdentityProviderUserLookupFailed = "IdentityProvider.UserLookupFailed";
    public const string ManagerProfileEmailRequired = "ManagerProfile.EmailRequired";
    public const string ManagerProfileFullNameRequired = "ManagerProfile.FullNameRequired";
    public const string ManagerProfileKeycloakUserIdRequired = "ManagerProfile.KeycloakUserIdRequired";
    public const string ProfileNotFound = "Profile.NotFound";
    public const string ValidationFailed = "Validation.Failed";
}
