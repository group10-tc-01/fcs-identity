using Fcs.Identity.Application.Abstractions.Identity;
using Fcs.Identity.Application.Audit;
using Fcs.Identity.Application.IntegrationEvents.AuditLogs;
using Fcs.Identity.Application.UseCases.Auth.Login;
using Fcs.Identity.CommomTestsUtilities.Builders.DonorProfiles;
using Fcs.Identity.CommomTestsUtilities.TestDoubles;
using Fcs.Identity.Domain.Shared;
using Fcs.Identity.Domain.Shared.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fcs.Identity.UnitTests.Application.UseCases.Auth.Login;

public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task Given_Handle_Called_When_CredentialsAreValid_Then_ShouldReturnTokenResponse()
    {
        // Arrange
        var identityProvider = new FakeIdentityProvider();
        const string keycloakUserId = "keycloak-donor-id";
        identityProvider.ConfigureLoginResult(new LoginIdentityUserResponse(
            "access-token",
            "refresh-token",
            300,
            "Bearer",
            keycloakUserId,
            [IdentityRoles.Donor]));
        var messagePublisher = new FakeMessagePublisher();
        var donorProfileRepository = new InMemoryDonorProfileRepository();
        var donorProfile = new DonorProfileBuilder()
            .WithKeycloakUserId(keycloakUserId)
            .WithEmail("doador@email.com")
            .Build();
        await donorProfileRepository.AddAsync(donorProfile);
        var handler = new LoginCommandHandler(
            identityProvider,
            messagePublisher,
            donorProfileRepository,
            new InMemoryManagerProfileRepository(),
            NullLogger<LoginCommandHandler>.Instance);
        var command = new LoginCommand("doador@email.com", "Password123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresIn.Should().Be(300);
        result.Value.TokenType.Should().Be("Bearer");
        identityProvider.LoginCalls.Should().Be(1);
        identityProvider.LastLoginRequest.Should().BeEquivalentTo(new LoginIdentityUserRequest(command.Email, command.Password));
        var auditMessage = await messagePublisher.WaitForSingleMessageAsync<AuditLogRequestedEvent>();
        auditMessage.Action.Should().Be(AuditActions.LoginSucceeded);
        auditMessage.EntityName.Should().Be("Authentication");
        auditMessage.ActorId.Should().Be(donorProfile.Id);
        auditMessage.ActorType.Should().Be(IdentityRoles.Donor);
    }

    [Fact]
    public async Task Given_Handle_Called_When_IdentityProviderFails_Then_ShouldReturnFailure()
    {
        // Arrange
        var identityProvider = new FakeIdentityProvider();
        identityProvider.ConfigureLoginResult(Error.Unauthorized("IdentityProvider.InvalidCredentials", "Invalid email or password."));
        var messagePublisher = new FakeMessagePublisher();
        var handler = new LoginCommandHandler(
            identityProvider,
            messagePublisher,
            new InMemoryDonorProfileRepository(),
            new InMemoryManagerProfileRepository(),
            NullLogger<LoginCommandHandler>.Instance);
        var command = new LoginCommand("doador@email.com", "wrong-password");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IdentityProvider.InvalidCredentials");
        identityProvider.LoginCalls.Should().Be(1);
        var auditMessage = await messagePublisher.WaitForSingleMessageAsync<AuditLogRequestedEvent>();
        auditMessage.Action.Should().Be(AuditActions.LoginFailed);
        auditMessage.EntityName.Should().Be("Authentication");
        auditMessage.ActorType.Should().Be("Public");
    }
}
