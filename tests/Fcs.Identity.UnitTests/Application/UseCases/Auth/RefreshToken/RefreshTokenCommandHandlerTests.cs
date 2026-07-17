using Fcs.Identity.Application.Abstractions.Identity;
using Fcs.Identity.Application.Audit;
using Fcs.Identity.Application.IntegrationEvents.AuditLogs;
using Fcs.Identity.Application.UseCases.Auth.RefreshToken;
using Fcs.Identity.CommomTestsUtilities.TestDoubles;
using Fcs.Identity.Domain.ManagerProfiles;
using Fcs.Identity.Domain.Shared;
using Fcs.Identity.Domain.Shared.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fcs.Identity.UnitTests.Application.UseCases.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Given_Handle_Called_When_RefreshTokenIsValid_Then_ShouldReturnTokenResponse()
    {
        // Arrange
        var identityProvider = new FakeIdentityProvider();
        const string keycloakUserId = "keycloak-manager-id";
        identityProvider.ConfigureRefreshTokenResult(new LoginIdentityUserResponse(
            "new-access-token",
            "new-refresh-token",
            300,
            "Bearer",
            keycloakUserId,
            [IdentityRoles.Manager]));
        var messagePublisher = new FakeMessagePublisher();
        var managerProfileRepository = new InMemoryManagerProfileRepository();
        var managerProfile = ManagerProfile.Create(keycloakUserId, "Gestor ONG", "gestor@email.com").Value;
        await managerProfileRepository.AddAsync(managerProfile);
        var handler = new RefreshTokenCommandHandler(
            identityProvider,
            messagePublisher,
            new InMemoryDonorProfileRepository(),
            managerProfileRepository,
            NullLogger<RefreshTokenCommandHandler>.Instance);
        var command = new RefreshTokenCommand("refresh-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        result.Value.ExpiresIn.Should().Be(300);
        result.Value.TokenType.Should().Be("Bearer");
        identityProvider.RefreshTokenCalls.Should().Be(1);
        identityProvider.LastRefreshTokenRequest.Should().BeEquivalentTo(new RefreshTokenIdentityUserRequest(command.RefreshToken));
        var auditMessage = await messagePublisher.WaitForSingleMessageAsync<AuditLogRequestedEvent>();
        auditMessage.Action.Should().Be(AuditActions.TokenRefreshed);
        auditMessage.EntityName.Should().Be("Authentication");
        auditMessage.ActorId.Should().Be(managerProfile.Id);
        auditMessage.ActorType.Should().Be(IdentityRoles.Manager);
    }

    [Fact]
    public async Task Given_Handle_Called_When_RefreshTokenIsInvalid_Then_ShouldReturnUnauthorized()
    {
        // Arrange
        var identityProvider = new FakeIdentityProvider();
        identityProvider.ConfigureRefreshTokenResult(Error.Unauthorized("IdentityProvider.InvalidRefreshToken", "Invalid refresh token."));
        var messagePublisher = new FakeMessagePublisher();
        var handler = new RefreshTokenCommandHandler(
            identityProvider,
            messagePublisher,
            new InMemoryDonorProfileRepository(),
            new InMemoryManagerProfileRepository(),
            NullLogger<RefreshTokenCommandHandler>.Instance);
        var command = new RefreshTokenCommand("invalid-refresh-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IdentityProvider.InvalidRefreshToken");
        identityProvider.RefreshTokenCalls.Should().Be(1);
    }
}
