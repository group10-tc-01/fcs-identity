using System.Net;
using System.Net.Http.Json;
using Fcg.Identity.Application.UseCases.Auth.Login;
using Fcg.Identity.Application.UseCases.Auth.RefreshToken;
using Fcg.Identity.Application.UseCases.Donors.RegisterDonor;
using Fcg.Identity.CommomTestsUtilities.Builders.DonorProfiles;
using Fcg.Identity.CommomTestsUtilities.Builders.Donors;
using Fcg.Identity.Domain.Abstractions;
using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Domain.Shared.Results;
using Fcg.Identity.IntegratedTests.Configurations;
using Fcg.Identity.IntegratedTests.Support;
using Fcg.Identity.WebApi.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Identity.IntegratedTests.WebApi.Controllers.v1;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync()
    {
        return _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [DockerAvailableFact]
    public async Task Given_RegisterDonorEndpoint_Called_When_RequestIsValid_Then_ShouldReturnCreated()
    {
        // Arrange
        var command = new RegisterDonorCommandBuilder().Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register/donor", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterDonorResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Email.Should().Be(command.Email);
        _factory.IdentityProvider.CreateDonorCalls.Should().Be(1);
    }

    [DockerAvailableFact]
    public async Task Given_RegisterDonorEndpoint_Called_When_RequestIsInvalid_Then_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new RegisterDonorCommandBuilder()
            .WithFullName(string.Empty)
            .WithEmail("invalid-email")
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register/donor", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        _factory.IdentityProvider.CreateDonorCalls.Should().Be(0);
    }

    [DockerAvailableFact]
    public async Task Given_RegisterDonorEndpoint_Called_When_EmailAlreadyExists_Then_ShouldReturnConflict()
    {
        // Arrange
        var existingDonorProfile = new DonorProfileBuilder().Build();
        await SaveDonorProfileAsync(existingDonorProfile);
        var command = new RegisterDonorCommandBuilder()
            .WithEmail(existingDonorProfile.Email.Value)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register/donor", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        payload.Message.Should().Be("A donor profile with this email already exists.");
        _factory.IdentityProvider.CreateDonorCalls.Should().Be(0);
    }

    [DockerAvailableFact]
    public async Task Given_RegisterDonorEndpoint_Called_When_CpfAlreadyExists_Then_ShouldReturnConflict()
    {
        // Arrange
        var existingDonorProfile = new DonorProfileBuilder().Build();
        await SaveDonorProfileAsync(existingDonorProfile);
        var command = new RegisterDonorCommandBuilder()
            .WithCpf(existingDonorProfile.Cpf.Value)
            .Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register/donor", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        payload.Message.Should().Be("A donor profile with this CPF already exists.");
        _factory.IdentityProvider.CreateDonorCalls.Should().Be(0);
    }

    [DockerAvailableFact]
    public async Task Given_RegisterDonorEndpoint_Called_When_IdentityProviderReturnsConflict_Then_ShouldReturnConflict()
    {
        // Arrange
        _factory.IdentityProvider.ConfigureCreateDonorResult(
            Error.Conflict("IdentityProvider.UserAlreadyExists", "A user with this email already exists in the identity provider."));
        var command = new RegisterDonorCommandBuilder().Build();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register/donor", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        payload.Message.Should().Be("A user with this email already exists in the identity provider.");
        _factory.IdentityProvider.CreateDonorCalls.Should().Be(1);
    }

    [DockerAvailableFact]
    public async Task Given_LoginEndpoint_Called_When_RequestIsValid_Then_ShouldReturnOk()
    {
        // Arrange
        var command = new LoginCommand("doador@email.com", "Password123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.AccessToken.Should().Be("access-token");
        _factory.IdentityProvider.LoginCalls.Should().Be(1);
    }

    [DockerAvailableFact]
    public async Task Given_LoginEndpoint_Called_When_RequestIsInvalid_Then_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new LoginCommand("invalid-email", string.Empty);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        _factory.IdentityProvider.LoginCalls.Should().Be(0);
    }

    [DockerAvailableFact]
    public async Task Given_LoginEndpoint_Called_When_CredentialsAreInvalid_Then_ShouldReturnUnauthorized()
    {
        // Arrange
        _factory.IdentityProvider.ConfigureLoginResult(
            Error.Unauthorized("IdentityProvider.InvalidCredentials", "Invalid email or password."));
        var command = new LoginCommand("doador@email.com", "wrong-password");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        payload.Message.Should().Be("Invalid email or password.");
        _factory.IdentityProvider.LoginCalls.Should().Be(1);
    }

    [DockerAvailableFact]
    public async Task Given_RefreshEndpoint_Called_When_RequestIsValid_Then_ShouldReturnOk()
    {
        // Arrange
        var command = new RefreshTokenCommand("refresh-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.AccessToken.Should().Be("new-access-token");
        _factory.IdentityProvider.RefreshTokenCalls.Should().Be(1);
    }

    [DockerAvailableFact]
    public async Task Given_RefreshEndpoint_Called_When_RequestIsInvalid_Then_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new RefreshTokenCommand(string.Empty);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        _factory.IdentityProvider.RefreshTokenCalls.Should().Be(0);
    }

    [DockerAvailableFact]
    public async Task Given_RefreshEndpoint_Called_When_RefreshTokenIsInvalid_Then_ShouldReturnUnauthorized()
    {
        // Arrange
        _factory.IdentityProvider.ConfigureRefreshTokenResult(
            Error.Unauthorized("IdentityProvider.InvalidRefreshToken", "Invalid refresh token."));
        var command = new RefreshTokenCommand("invalid-refresh-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", command);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        payload.Message.Should().Be("Invalid refresh token.");
        _factory.IdentityProvider.RefreshTokenCalls.Should().Be(1);
    }

    private async Task SaveDonorProfileAsync(DonorProfile donorProfile)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDonorProfileRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await repository.AddAsync(donorProfile, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}
