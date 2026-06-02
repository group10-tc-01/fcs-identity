using System.Net;
using System.Text.Json;
using Fcg.Identity.Application.Abstractions.Identity;
using Fcg.Identity.Domain.Shared;
using Fcg.Identity.Domain.Shared.Results;
using Fcg.Identity.Infrastructure.Keycloak.Http;
using Fcg.Identity.Infrastructure.Keycloak.Http.Contracts;
using Fcg.Identity.Infrastructure.Keycloak.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;

namespace Fcg.Identity.Infrastructure.Keycloak.Identity;

public sealed class KeycloakIdentityProvider : IIdentityProvider
{
    private readonly IKeycloakApi _keycloakApi;
    private readonly KeycloakSettings _keycloakSettings;
    private readonly ILogger<KeycloakIdentityProvider> _logger;

    public KeycloakIdentityProvider(
        IKeycloakApi keycloakApi,
        IOptions<KeycloakSettings> settings,
        ILogger<KeycloakIdentityProvider> logger)
    {
        _keycloakApi = keycloakApi;
        _keycloakSettings = settings.Value;
        _logger = logger;
    }

    public async Task<Result<CreateDonorIdentityUserResponse>> CreateDonorAsync(CreateDonorIdentityUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Keycloak donor creation started. Email: {Email}. Realm: {Realm}", request.Email, _keycloakSettings.Realm);

            var accessTokenResult = await GetAdminAccessTokenAsync(cancellationToken);
            if (accessTokenResult.IsFailure)
            {
                _logger.LogWarning(
                    "Keycloak donor creation stopped because admin authentication failed. ErrorCode: {ErrorCode}",
                    accessTokenResult.Error.Code);

                return accessTokenResult.Error;
            }

            var authorization = Bearer(accessTokenResult.Value);
            _logger.LogInformation("Creating Keycloak user. Email: {Email}. Realm: {Realm}", request.Email, _keycloakSettings.Realm);

            var createUserResponse = await _keycloakApi.CreateUserAsync(
                _keycloakSettings.Realm,
                authorization,
                CreateUserRequest(request),
                cancellationToken);

            if (createUserResponse.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogWarning("Keycloak user creation returned conflict. Email: {Email}", request.Email);
                return Error.Conflict("IdentityProvider.UserAlreadyExists", "A user with this email already exists in the identity provider.");
            }

            if (!createUserResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Keycloak user creation failed. Email: {Email}. StatusCode: {StatusCode}",
                    request.Email,
                    createUserResponse.StatusCode);

                return Error.Failure("IdentityProvider.CreateUserFailed", "Could not create donor user in the identity provider.");
            }

            _logger.LogInformation("Looking up created Keycloak user by email {Email}", request.Email);
            var keycloakUserIdResult = await FindUserIdByEmailAsync(request.Email, authorization, cancellationToken);
            if (keycloakUserIdResult.IsFailure)
            {
                _logger.LogWarning(
                    "Keycloak user lookup failed after creation. Email: {Email}. ErrorCode: {ErrorCode}",
                    request.Email,
                    keycloakUserIdResult.Error.Code);

                return keycloakUserIdResult.Error;
            }

            var keycloakUserId = keycloakUserIdResult.Value;
            _logger.LogInformation(
                "Assigning Keycloak realm role {RoleName}. KeycloakUserId: {KeycloakUserId}",
                IdentityRoles.Donor,
                keycloakUserId);

            var assignRoleResult = await AssignRealmRoleAsync(keycloakUserId, IdentityRoles.Donor, authorization, cancellationToken);
            if (assignRoleResult.IsFailure)
            {
                _logger.LogWarning(
                    "Keycloak role assignment failed. Starting compensating delete. KeycloakUserId: {KeycloakUserId}. ErrorCode: {ErrorCode}",
                    keycloakUserId,
                    assignRoleResult.Error.Code);

                await DeleteUserAsync(keycloakUserId, authorization, cancellationToken);
                return assignRoleResult.Error;
            }

            _logger.LogInformation(
                "Keycloak donor creation completed. Email: {Email}. KeycloakUserId: {KeycloakUserId}",
                request.Email,
                keycloakUserId);

            return new CreateDonorIdentityUserResponse(keycloakUserId);
        }
        catch (ApiException exception)
        {
            _logger.LogError(exception, "Keycloak donor creation failed because provider API is unavailable. Email: {Email}", request.Email);
            return Error.Failure("IdentityProvider.Unavailable", "The identity provider is unavailable.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Keycloak donor creation failed because provider HTTP request failed. Email: {Email}", request.Email);
            return Error.Failure("IdentityProvider.Unavailable", "The identity provider is unavailable.");
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogError(exception, "Keycloak donor creation timed out. Email: {Email}", request.Email);
            return Error.Failure("IdentityProvider.Timeout", "The identity provider request timed out.");
        }
    }

    public async Task<Result<LoginIdentityUserResponse>> LoginAsync(LoginIdentityUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Keycloak login started. Email: {Email}. Realm: {Realm}", request.Email, _keycloakSettings.Realm);

            var tokenResponse = await _keycloakApi.GetTokenAsync(
                _keycloakSettings.Realm,
                new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = _keycloakSettings.ClientId,
                    ["username"] = request.Email,
                    ["password"] = request.Password
                },
                cancellationToken);

            if (tokenResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Keycloak login returned unauthorized. Email: {Email}", request.Email);
                return Error.Unauthorized("IdentityProvider.InvalidCredentials", "Invalid email or password.");
            }

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Keycloak login failed. Email: {Email}. StatusCode: {StatusCode}",
                    request.Email,
                    tokenResponse.StatusCode);

                return Error.Failure("IdentityProvider.LoginFailed", "Could not authenticate with the identity provider.");
            }

            _logger.LogInformation("Keycloak login completed. Email: {Email}. StatusCode: {StatusCode}", request.Email, tokenResponse.StatusCode);

            return MapTokenResponse(tokenResponse.Content);
        }
        catch (ApiException exception)
        {
            _logger.LogError(exception, "Keycloak login failed because provider API is unavailable. Email: {Email}", request.Email);
            return Error.Failure("IdentityProvider.Unavailable", "The identity provider is unavailable.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Keycloak login failed because provider HTTP request failed. Email: {Email}", request.Email);
            return Error.Failure("IdentityProvider.Unavailable", "The identity provider is unavailable.");
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogError(exception, "Keycloak login timed out. Email: {Email}", request.Email);
            return Error.Failure("IdentityProvider.Timeout", "The identity provider request timed out.");
        }
    }

    public async Task<Result<LoginIdentityUserResponse>> RefreshTokenAsync(RefreshTokenIdentityUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Keycloak refresh token started. Realm: {Realm}", _keycloakSettings.Realm);

            var tokenResponse = await _keycloakApi.GetTokenAsync(
                _keycloakSettings.Realm,
                new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = _keycloakSettings.ClientId,
                    ["refresh_token"] = request.RefreshToken
                },
                cancellationToken);

            if (tokenResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Keycloak refresh token returned unauthorized");
                return Error.Unauthorized("IdentityProvider.InvalidRefreshToken", "Invalid refresh token.");
            }

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Keycloak refresh token failed. StatusCode: {StatusCode}", tokenResponse.StatusCode);
                return Error.Failure("IdentityProvider.RefreshTokenFailed", "Could not refresh token with the identity provider.");
            }

            _logger.LogInformation("Keycloak refresh token completed. StatusCode: {StatusCode}", tokenResponse.StatusCode);

            return MapTokenResponse(tokenResponse.Content);
        }
        catch (ApiException exception)
        {
            _logger.LogError(exception, "Keycloak refresh token failed because provider API is unavailable");
            return Error.Failure("IdentityProvider.Unavailable", "The identity provider is unavailable.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Keycloak refresh token failed because provider HTTP request failed");
            return Error.Failure("IdentityProvider.Unavailable", "The identity provider is unavailable.");
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogError(exception, "Keycloak refresh token timed out");
            return Error.Failure("IdentityProvider.Timeout", "The identity provider request timed out.");
        }
    }

    private async Task<Result<string>> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Authenticating with Keycloak admin API. AdminRealm: {AdminRealm}. AdminClientId: {AdminClientId}. AdminUsername: {AdminUsername}",
            _keycloakSettings.AdminRealm,
            _keycloakSettings.AdminClientId,
            _keycloakSettings.AdminUsername);

        var response = await _keycloakApi.GetTokenAsync(
            _keycloakSettings.AdminRealm,
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = _keycloakSettings.AdminClientId,
                ["username"] = _keycloakSettings.AdminUsername,
                ["password"] = _keycloakSettings.AdminPassword
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Keycloak admin authentication failed. StatusCode: {StatusCode}", response.StatusCode);
            return Error.Failure("IdentityProvider.AdminAuthenticationFailed", "Could not authenticate with the identity provider admin API.");
        }

        if (response.Content is null || string.IsNullOrWhiteSpace(response.Content.AccessToken))
        {
            _logger.LogWarning("Keycloak admin authentication did not return an access token");
            return Error.Failure("IdentityProvider.AdminTokenMissing", "The identity provider did not return an admin access token.");
        }

        _logger.LogInformation("Keycloak admin authentication completed. StatusCode: {StatusCode}", response.StatusCode);

        return response.Content.AccessToken;
    }

    private async Task<Result<string>> FindUserIdByEmailAsync(
        string email,
        string authorization,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Finding Keycloak user by email {Email}. Realm: {Realm}", email, _keycloakSettings.Realm);

        var response = await _keycloakApi.FindUsersAsync(_keycloakSettings.Realm, authorization, email, exact: true, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Keycloak user lookup failed. Email: {Email}. StatusCode: {StatusCode}",
                email,
                response.StatusCode);

            return Error.Failure("IdentityProvider.UserLookupFailed", "Could not find the created user in the identity provider.");
        }

        var users = response.Content ?? [];
        var user = users.SingleOrDefault();
        if (user is null || string.IsNullOrWhiteSpace(user.Id))
        {
            _logger.LogWarning("Keycloak user lookup returned no single user. Email: {Email}. Count: {UserCount}", email, users.Count);
            return Error.Failure("IdentityProvider.UserLookupFailed", "Could not find the created user in the identity provider.");
        }

        _logger.LogInformation("Keycloak user lookup completed. Email: {Email}. KeycloakUserId: {KeycloakUserId}", email, user.Id);

        return user.Id;
    }

    private async Task<Result<string>> AssignRealmRoleAsync(
        string keycloakUserId,
        string roleName,
        string authorization,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Resolving Keycloak realm role {RoleName}. Realm: {Realm}",
            roleName,
            _keycloakSettings.Realm);

        var roleResponse = await _keycloakApi.GetRealmRoleAsync(_keycloakSettings.Realm, roleName, authorization, cancellationToken);

        if (roleResponse.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Keycloak role {RoleName} was not found. Realm: {Realm}", roleName, _keycloakSettings.Realm);
            return Error.Failure("IdentityProvider.RoleNotFound", $"The role '{roleName}' was not found in the identity provider.");
        }

        if (!roleResponse.IsSuccessStatusCode || roleResponse.Content is null || string.IsNullOrWhiteSpace(roleResponse.Content.Name))
        {
            _logger.LogWarning(
                "Keycloak role resolution failed. RoleName: {RoleName}. StatusCode: {StatusCode}",
                roleName,
                roleResponse.StatusCode);

            return Error.Failure("IdentityProvider.GetRoleFailed", "Could not resolve the donor role in the identity provider.");
        }

        _logger.LogInformation(
            "Assigning Keycloak realm role {RoleName} to user {KeycloakUserId}",
            roleName,
            keycloakUserId);

        var assignResponse = await _keycloakApi.AssignRealmRolesAsync(
            _keycloakSettings.Realm,
            keycloakUserId,
            authorization,
            [roleResponse.Content],
            cancellationToken);

        if (!assignResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Keycloak role assignment failed. RoleName: {RoleName}. KeycloakUserId: {KeycloakUserId}. StatusCode: {StatusCode}",
                roleName,
                keycloakUserId,
                assignResponse.StatusCode);

            return Error.Failure("IdentityProvider.AssignRoleFailed", "Could not assign the donor role in the identity provider.");
        }

        _logger.LogInformation("Keycloak role assignment completed. RoleName: {RoleName}. KeycloakUserId: {KeycloakUserId}", roleName, keycloakUserId);

        return keycloakUserId;
    }

    private async Task DeleteUserAsync(string keycloakUserId, string authorization, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting Keycloak user as compensation. KeycloakUserId: {KeycloakUserId}", keycloakUserId);
        await _keycloakApi.DeleteUserAsync(_keycloakSettings.Realm, keycloakUserId, authorization, cancellationToken);
        _logger.LogInformation("Compensating Keycloak user delete completed. KeycloakUserId: {KeycloakUserId}", keycloakUserId);
    }

    private static CreateKeycloakUserRequest CreateUserRequest(CreateDonorIdentityUserRequest request)
    {
        var (firstName, lastName) = SplitFullName(request.FullName);

        return new CreateKeycloakUserRequest(
            request.Email,
            request.Email,
            firstName,
            lastName,
            Enabled: true,
            EmailVerified: true,
            Credentials:
            [
                new KeycloakCredential("password", request.Password, Temporary: false)
            ],
            RequiredActions: []);
    }

    private static Result<LoginIdentityUserResponse> MapTokenResponse(KeycloakTokenResponse? tokenResponse)
    {
        if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            return Error.Failure("IdentityProvider.TokenMissing", "The identity provider did not return an access token.");
        }

        var tokenIdentity = ExtractTokenIdentity(tokenResponse.AccessToken);

        return new LoginIdentityUserResponse(
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken ?? string.Empty,
            tokenResponse.ExpiresIn,
            tokenResponse.TokenType,
            tokenIdentity.KeycloakUserId,
            tokenIdentity.Roles);
    }

    private static TokenIdentity ExtractTokenIdentity(string accessToken)
    {
        try
        {
            var parts = accessToken.Split('.');
            if (parts.Length < 2)
            {
                return TokenIdentity.Empty;
            }

            using var payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            var root = payload.RootElement;
            var keycloakUserId = root.TryGetProperty("sub", out var subject) && subject.ValueKind == JsonValueKind.String
                ? subject.GetString()
                : null;

            var roles = new List<string>();
            AddRoles(root, "roles", roles);
            AddRoles(root, "role", roles);

            if (root.TryGetProperty("realm_access", out var realmAccess) && realmAccess.ValueKind == JsonValueKind.Object)
            {
                AddRoles(realmAccess, "roles", roles);
            }

            return new TokenIdentity(keycloakUserId, roles.Distinct(StringComparer.Ordinal).ToArray());
        }
        catch (FormatException)
        {
            return TokenIdentity.Empty;
        }
        catch (JsonException)
        {
            return TokenIdentity.Empty;
        }
    }

    private static void AddRoles(JsonElement root, string propertyName, ICollection<string> roles)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            var role = property.GetString();
            if (!string.IsNullOrWhiteSpace(role))
            {
                roles.Add(role);
            }

            return;
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var role in property.EnumerateArray())
        {
            if (role.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(role.GetString()))
            {
                roles.Add(role.GetString()!);
            }
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        var padding = base64.Length % 4;
        if (padding > 0)
        {
            base64 = base64.PadRight(base64.Length + 4 - padding, '=');
        }

        return Convert.FromBase64String(base64);
    }

    private sealed record TokenIdentity(string? KeycloakUserId, IReadOnlyCollection<string> Roles)
    {
        public static TokenIdentity Empty { get; } = new(null, []);
    }

    private static (string FirstName, string LastName) SplitFullName(string fullName)
    {
        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (nameParts.Length == 1)
        {
            return (nameParts[0], nameParts[0]);
        }

        return (nameParts[0], string.Join(' ', nameParts.Skip(1)));
    }

    private static string Bearer(string token) => $"Bearer {token}";
}
