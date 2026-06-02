using System.Net;
using System.Text.Json;
using Fcg.Identity.Application.Abstractions.Identity;
using Fcg.Identity.Domain.Shared;
using Fcg.Identity.Domain.Shared.Results;
using Fcg.Identity.Infrastructure.Keycloak.Http;
using Fcg.Identity.Infrastructure.Keycloak.Http.Contracts;
using Fcg.Identity.Infrastructure.Keycloak.Identity;
using Fcg.Identity.Infrastructure.Keycloak.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Refit;

namespace Fcg.Identity.UnitTests.Infrastructure.Keycloak.Identity;

public sealed class KeycloakIdentityProviderTests
{
    [Fact]
    public async Task Given_CreateDonorAsync_Called_When_KeycloakFlowSucceeds_Then_ShouldReturnCreatedUserId()
    {
        // Arrange
        var keycloakApi = new FakeKeycloakApi();
        var provider = CreateProvider(keycloakApi);
        var request = new CreateDonorIdentityUserRequest("Maria Silva", "maria@email.com", "StrongPassword123!");

        // Act
        var result = await provider.CreateDonorAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.KeycloakUserId.Should().Be(keycloakApi.CreatedUserId);
        keycloakApi.CreateUserCalls.Should().Be(1);
        keycloakApi.LastCreateUserRequest.Should().NotBeNull();
        keycloakApi.LastCreateUserRequest!.FirstName.Should().Be("Maria");
        keycloakApi.LastCreateUserRequest.LastName.Should().Be("Silva");
        keycloakApi.LastCreateUserRequest.EmailVerified.Should().BeTrue();
        keycloakApi.LastCreateUserRequest.RequiredActions.Should().BeEmpty();
        keycloakApi.LastCreateUserRequest.Credentials.Should().ContainSingle(credential =>
            credential.Type == "password" &&
            credential.Value == request.Password &&
            !credential.Temporary);
        keycloakApi.FindUsersCalls.Should().Be(1);
        keycloakApi.AssignRealmRolesCalls.Should().Be(1);
        keycloakApi.DeleteUserCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_CreateDonorAsync_Called_When_FullNameHasSingleName_Then_ShouldCreateUserWithNonEmptyLastName()
    {
        // Arrange
        var keycloakApi = new FakeKeycloakApi();
        var provider = CreateProvider(keycloakApi);
        var request = new CreateDonorIdentityUserRequest("Madonna", "madonna@email.com", "StrongPassword123!");

        // Act
        var result = await provider.CreateDonorAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        keycloakApi.LastCreateUserRequest.Should().NotBeNull();
        keycloakApi.LastCreateUserRequest!.FirstName.Should().Be("Madonna");
        keycloakApi.LastCreateUserRequest.LastName.Should().Be("Madonna");
    }

    [Fact]
    public async Task Given_CreateDonorAsync_Called_When_AdminTokenFails_Then_ShouldReturnFailure()
    {
        // Arrange
        var keycloakApi = new FakeKeycloakApi
        {
            AdminTokenResponse = CreateApiResponse<KeycloakTokenResponse>(HttpStatusCode.Unauthorized, null)
        };
        var provider = CreateProvider(keycloakApi);

        // Act
        var result = await provider.CreateDonorAsync(CreateDonorRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IdentityProvider.AdminAuthenticationFailed");
        keycloakApi.CreateUserCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_CreateDonorAsync_Called_When_CreateUserConflicts_Then_ShouldReturnConflict()
    {
        // Arrange
        var keycloakApi = new FakeKeycloakApi
        {
            CreateUserResponse = CreateApiResponse(HttpStatusCode.Conflict)
        };
        var provider = CreateProvider(keycloakApi);

        // Act
        var result = await provider.CreateDonorAsync(CreateDonorRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IdentityProvider.UserAlreadyExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        keycloakApi.FindUsersCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_CreateDonorAsync_Called_When_UserLookupFails_Then_ShouldReturnFailure()
    {
        // Arrange
        var keycloakApi = new FakeKeycloakApi
        {
            FindUsersResponse = CreateApiResponse<List<KeycloakUserResponse>>(HttpStatusCode.OK, [])
        };
        var provider = CreateProvider(keycloakApi);

        // Act
        var result = await provider.CreateDonorAsync(CreateDonorRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IdentityProvider.UserLookupFailed");
        keycloakApi.AssignRealmRolesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_CreateDonorAsync_Called_When_RoleAssignmentFails_Then_ShouldDeleteCreatedUserAndReturnFailure()
    {
        // Arrange
        var keycloakApi = new FakeKeycloakApi
        {
            AssignRealmRolesResponse = CreateApiResponse(HttpStatusCode.BadRequest)
        };
        var provider = CreateProvider(keycloakApi);

        // Act
        var result = await provider.CreateDonorAsync(CreateDonorRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IdentityProvider.AssignRoleFailed");
        keycloakApi.DeleteUserCalls.Should().Be(1);
    }

    [Fact]
    public async Task Given_LoginAsync_Called_When_KeycloakReturnsToken_Then_ShouldReturnToken()
    {
        // Arrange
        var keycloakApi = new FakeKeycloakApi();
        keycloakApi.LoginTokenResponse = CreateApiResponse(
            HttpStatusCode.OK,
            new KeycloakTokenResponse(CreateJwt("keycloak-user-id", [IdentityRoles.Donor]), "refresh-token", 300, "Bearer"));
        var provider = CreateProvider(keycloakApi);
        var request = new LoginIdentityUserRequest("maria@email.com", "StrongPassword123!");

        // Act
        var result = await provider.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresIn.Should().Be(300);
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.KeycloakUserId.Should().Be("keycloak-user-id");
        result.Value.Roles.Should().Contain(IdentityRoles.Donor);
    }

    [Fact]
    public async Task Given_LoginAsync_Called_When_KeycloakReturnsUnauthorized_Then_ShouldReturnUnauthorized()
    {
        // Arrange
        var keycloakApi = new FakeKeycloakApi
        {
            LoginTokenResponse = CreateApiResponse<KeycloakTokenResponse>(HttpStatusCode.Unauthorized, null)
        };
        var provider = CreateProvider(keycloakApi);

        // Act
        var result = await provider.LoginAsync(new LoginIdentityUserRequest("maria@email.com", "wrong-password"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IdentityProvider.InvalidCredentials");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Given_RefreshTokenAsync_Called_When_KeycloakReturnsToken_Then_ShouldReturnToken()
    {
        // Arrange
        var keycloakApi = new FakeKeycloakApi();
        var provider = CreateProvider(keycloakApi);
        var request = new RefreshTokenIdentityUserRequest("refresh-token");

        // Act
        var result = await provider.RefreshTokenAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        keycloakApi.LastTokenForm.Should().NotBeNull();
        keycloakApi.LastTokenForm!["grant_type"].Should().Be("refresh_token");
        keycloakApi.LastTokenForm["client_id"].Should().Be("solidarity-api");
        keycloakApi.LastTokenForm["refresh_token"].Should().Be(request.RefreshToken);
    }

    [Fact]
    public async Task Given_RefreshTokenAsync_Called_When_KeycloakReturnsUnauthorized_Then_ShouldReturnUnauthorized()
    {
        // Arrange
        var keycloakApi = new FakeKeycloakApi
        {
            LoginTokenResponse = CreateApiResponse<KeycloakTokenResponse>(HttpStatusCode.Unauthorized, null)
        };
        var provider = CreateProvider(keycloakApi);

        // Act
        var result = await provider.RefreshTokenAsync(new RefreshTokenIdentityUserRequest("invalid-refresh-token"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IdentityProvider.InvalidRefreshToken");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    private static CreateDonorIdentityUserRequest CreateDonorRequest()
    {
        return new CreateDonorIdentityUserRequest("Maria Silva", "maria@email.com", "StrongPassword123!");
    }

    private static KeycloakIdentityProvider CreateProvider(IKeycloakApi keycloakApi)
    {
        return new KeycloakIdentityProvider(
            keycloakApi,
            Options.Create(new KeycloakSettings
            {
                BaseUrl = "http://localhost:8081",
                Realm = "conexao-solidaria",
                ClientId = "solidarity-api",
                AdminRealm = "master",
                AdminClientId = "admin-cli",
                AdminUsername = "admin",
                AdminPassword = "admin"
            }),
            NullLogger<KeycloakIdentityProvider>.Instance);
    }

    private static ApiResponse<T> CreateApiResponse<T>(HttpStatusCode statusCode, T? content)
    {
        return new ApiResponse<T>(
            new HttpResponseMessage(statusCode),
            content,
            new RefitSettings(),
            error: null);
    }

    private static IApiResponse CreateApiResponse(HttpStatusCode statusCode)
    {
        return new ApiResponse<string>(
            new HttpResponseMessage(statusCode),
            string.Empty,
            new RefitSettings(),
            error: null);
    }

    private static string CreateJwt(string subject, IReadOnlyCollection<string> roles)
    {
        var header = Base64UrlEncode("""{"alg":"none","typ":"JWT"}"""u8.ToArray());
        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = subject,
            realm_access = new
            {
                roles
            }
        }));

        return $"{header}.{payload}.";
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class FakeKeycloakApi : IKeycloakApi
    {
        public string CreatedUserId { get; } = Guid.NewGuid().ToString();
        public int CreateUserCalls { get; private set; }
        public int FindUsersCalls { get; private set; }
        public int AssignRealmRolesCalls { get; private set; }
        public int DeleteUserCalls { get; private set; }
        public CreateKeycloakUserRequest? LastCreateUserRequest { get; private set; }
        public Dictionary<string, string>? LastTokenForm { get; private set; }

        public ApiResponse<KeycloakTokenResponse> AdminTokenResponse { get; set; } =
            CreateApiResponse(HttpStatusCode.OK, new KeycloakTokenResponse("admin-token", null, 300, "Bearer"));

        public ApiResponse<KeycloakTokenResponse> LoginTokenResponse { get; set; } =
            CreateApiResponse(HttpStatusCode.OK, new KeycloakTokenResponse("access-token", "refresh-token", 300, "Bearer"));

        public IApiResponse CreateUserResponse { get; set; } = CreateApiResponse(HttpStatusCode.Created);

        public ApiResponse<List<KeycloakUserResponse>> FindUsersResponse { get; set; }

        public ApiResponse<KeycloakRoleResponse> GetRealmRoleResponse { get; set; } =
            CreateApiResponse(HttpStatusCode.OK, new KeycloakRoleResponse("role-id", "Doador"));

        public IApiResponse AssignRealmRolesResponse { get; set; } = CreateApiResponse(HttpStatusCode.NoContent);

        public IApiResponse DeleteUserResponse { get; set; } = CreateApiResponse(HttpStatusCode.NoContent);

        public FakeKeycloakApi()
        {
            FindUsersResponse = CreateApiResponse(HttpStatusCode.OK, new List<KeycloakUserResponse> { new(CreatedUserId) });
        }

        public Task<ApiResponse<KeycloakTokenResponse>> GetTokenAsync(
            string realm,
            Dictionary<string, string> form,
            CancellationToken cancellationToken)
        {
            LastTokenForm = form;

            return Task.FromResult(realm == "master" ? AdminTokenResponse : LoginTokenResponse);
        }

        public Task<IApiResponse> CreateUserAsync(
            string realm,
            string authorization,
            CreateKeycloakUserRequest request,
            CancellationToken cancellationToken)
        {
            CreateUserCalls++;
            LastCreateUserRequest = request;
            return Task.FromResult(CreateUserResponse);
        }

        public Task<ApiResponse<List<KeycloakUserResponse>>> FindUsersAsync(
            string realm,
            string authorization,
            string email,
            bool exact,
            CancellationToken cancellationToken)
        {
            FindUsersCalls++;
            return Task.FromResult(FindUsersResponse);
        }

        public Task<IApiResponse> DeleteUserAsync(
            string realm,
            string userId,
            string authorization,
            CancellationToken cancellationToken)
        {
            DeleteUserCalls++;
            return Task.FromResult(DeleteUserResponse);
        }

        public Task<ApiResponse<KeycloakRoleResponse>> GetRealmRoleAsync(
            string realm,
            string roleName,
            string authorization,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GetRealmRoleResponse);
        }

        public Task<IApiResponse> AssignRealmRolesAsync(
            string realm,
            string userId,
            string authorization,
            IReadOnlyCollection<KeycloakRoleResponse> roles,
            CancellationToken cancellationToken)
        {
            AssignRealmRolesCalls++;
            return Task.FromResult(AssignRealmRolesResponse);
        }
    }
}
