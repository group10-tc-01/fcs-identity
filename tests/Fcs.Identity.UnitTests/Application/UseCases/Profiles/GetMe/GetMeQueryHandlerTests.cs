using Fcs.Identity.Application.Abstractions.Authentication;
using Fcs.Identity.Application.Audit;
using Fcs.Identity.Application.IntegrationEvents.AuditLogs;
using Fcs.Identity.Application.UseCases.Profiles.GetMe;
using Fcs.Identity.CommomTestsUtilities.Builders.DonorProfiles;
using Fcs.Identity.CommomTestsUtilities.TestDoubles;
using Fcs.Identity.Domain.ManagerProfiles;
using Fcs.Identity.Domain.Shared;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fcs.Identity.UnitTests.Application.UseCases.Profiles.GetMe;

public sealed class GetMeQueryHandlerTests
{
    [Fact]
    public async Task Given_Handle_Called_When_CurrentUserIsDonor_Then_ShouldReturnDonorProfile()
    {
        // Arrange
        var donorProfile = new DonorProfileBuilder().Build();
        var donorRepository = new InMemoryDonorProfileRepository();
        await donorRepository.AddAsync(donorProfile);
        var managerRepository = new InMemoryManagerProfileRepository();
        var currentUser = new FakeCurrentUser(donorProfile.KeycloakUserId, [IdentityRoles.Donor]);
        var messagePublisher = new FakeMessagePublisher();
        var handler = new GetMeQueryHandler(
            currentUser,
            donorRepository,
            managerRepository,
            messagePublisher,
            NullLogger<GetMeQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetMeQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(donorProfile.Id);
        result.Value.KeycloakUserId.Should().Be(donorProfile.KeycloakUserId);
        result.Value.Role.Should().Be(IdentityRoles.Donor);
        var auditMessage = await messagePublisher.WaitForSingleMessageAsync<AuditLogRequestedEvent>();
        auditMessage.Action.Should().Be(AuditActions.ProfileViewed);
        auditMessage.EntityName.Should().Be("DonorProfile");
        auditMessage.EntityId.Should().Be(donorProfile.Id.ToString());
        auditMessage.ActorId.Should().Be(donorProfile.Id);
        auditMessage.ActorType.Should().Be(IdentityRoles.Donor);
    }

    [Fact]
    public async Task Given_Handle_Called_When_CurrentUserIsManager_Then_ShouldReturnManagerProfile()
    {
        // Arrange
        var managerProfile = ManagerProfile.Create(Guid.NewGuid().ToString(), "Gestor ONG", "gestor@ong.test").Value;
        var donorRepository = new InMemoryDonorProfileRepository();
        var managerRepository = new InMemoryManagerProfileRepository();
        await managerRepository.AddAsync(managerProfile);
        var currentUser = new FakeCurrentUser(managerProfile.KeycloakUserId, [IdentityRoles.Manager]);
        var messagePublisher = new FakeMessagePublisher();
        var handler = new GetMeQueryHandler(
            currentUser,
            donorRepository,
            managerRepository,
            messagePublisher,
            NullLogger<GetMeQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetMeQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(managerProfile.Id);
        result.Value.KeycloakUserId.Should().Be(managerProfile.KeycloakUserId);
        result.Value.Role.Should().Be(IdentityRoles.Manager);
        var auditMessage = await messagePublisher.WaitForSingleMessageAsync<AuditLogRequestedEvent>();
        auditMessage.Action.Should().Be(AuditActions.ProfileViewed);
        auditMessage.EntityName.Should().Be("ManagerProfile");
        auditMessage.EntityId.Should().Be(managerProfile.Id.ToString());
        auditMessage.ActorId.Should().Be(managerProfile.Id);
        auditMessage.ActorType.Should().Be(IdentityRoles.Manager);
    }

    [Fact]
    public async Task Given_Handle_Called_When_ProfileDoesNotExist_Then_ShouldReturnNotFound()
    {
        // Arrange
        var donorRepository = new InMemoryDonorProfileRepository();
        var managerRepository = new InMemoryManagerProfileRepository();
        var currentUser = new FakeCurrentUser(Guid.NewGuid().ToString(), [IdentityRoles.Donor]);
        var handler = new GetMeQueryHandler(
            currentUser,
            donorRepository,
            managerRepository,
            new FakeMessagePublisher(),
            NullLogger<GetMeQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetMeQuery(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Profile.NotFound");
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(string? keycloakUserId, IReadOnlyCollection<string> roles)
        {
            KeycloakUserId = keycloakUserId;
            Roles = roles;
        }

        public bool IsAuthenticated => true;
        public string? KeycloakUserId { get; }
        public IReadOnlyCollection<string> Roles { get; }
    }
}
