using Fcg.Identity.Application.Abstractions.Identity;
using Fcg.Identity.Application.Audit;
using Fcg.Identity.Application.UseCases.Donors.RegisterDonor;
using Fcg.Identity.CommomTestsUtilities.Builders.DonorProfiles;
using Fcg.Identity.CommomTestsUtilities.Builders.Donors;
using Fcg.Identity.CommomTestsUtilities.TestDoubles;
using Fcg.Identity.Domain.Shared.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fcg.Identity.UnitTests.Application.UseCases.Donors.RegisterDonor;

public sealed class RegisterDonorCommandHandlerTests
{
    [Fact]
    public async Task Given_Handle_Called_When_CommandIsValid_Then_ShouldCreateDonorProfile()
    {
        // Arrange
        var donorProfileRepository = new InMemoryDonorProfileRepository();
        var messagePublisher = new FakeMessagePublisher();
        var identityProvider = new FakeIdentityProvider();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(donorProfileRepository, messagePublisher, identityProvider, unitOfWork);
        var command = new RegisterDonorCommandBuilder().Build();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.FullName.Should().Be(command.FullName);
        result.Value.Email.Should().Be(command.Email);
        result.Value.Cpf.Should().Contain("*");
        donorProfileRepository.DonorProfiles.Should().ContainSingle();
        var auditMessage = await messagePublisher.WaitForSingleMessageAsync<AuditLogRequestedEvent>();
        auditMessage.Action.Should().Be(AuditActions.DonorRegistered);
        auditMessage.EntityName.Should().Be("DonorProfile");
        auditMessage.ActorType.Should().Be("Doador");
        identityProvider.CreateDonorCalls.Should().Be(1);
        identityProvider.LastCreateDonorRequest.Should().BeEquivalentTo(new CreateDonorIdentityUserRequest(command.FullName, command.Email, command.Password));
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Given_Handle_Called_When_EmailAlreadyExists_Then_ShouldReturnConflict()
    {
        // Arrange
        var existingDonorProfile = new DonorProfileBuilder().Build();
        var donorProfileRepository = new InMemoryDonorProfileRepository();
        await donorProfileRepository.AddAsync(existingDonorProfile);
        var messagePublisher = new FakeMessagePublisher();
        var identityProvider = new FakeIdentityProvider();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(donorProfileRepository, messagePublisher, identityProvider, unitOfWork);
        var command = new RegisterDonorCommandBuilder()
            .WithEmail(existingDonorProfile.Email.Value)
            .Build();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DonorProfile.EmailAlreadyExists");
        identityProvider.CreateDonorCalls.Should().Be(0);
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_Handle_Called_When_CpfAlreadyExists_Then_ShouldReturnConflict()
    {
        // Arrange
        var existingDonorProfile = new DonorProfileBuilder().Build();
        var donorProfileRepository = new InMemoryDonorProfileRepository();
        await donorProfileRepository.AddAsync(existingDonorProfile);
        var messagePublisher = new FakeMessagePublisher();
        var identityProvider = new FakeIdentityProvider();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(donorProfileRepository, messagePublisher, identityProvider, unitOfWork);
        var command = new RegisterDonorCommandBuilder()
            .WithCpf(existingDonorProfile.Cpf.Value)
            .Build();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DonorProfile.CpfAlreadyExists");
        identityProvider.CreateDonorCalls.Should().Be(0);
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Given_Handle_Called_When_IdentityProviderFails_Then_ShouldReturnFailure()
    {
        // Arrange
        var donorProfileRepository = new InMemoryDonorProfileRepository();
        var messagePublisher = new FakeMessagePublisher();
        var identityProvider = new FakeIdentityProvider();
        identityProvider.ConfigureCreateDonorResult(Error.Failure("IdentityProvider.CreateUserFailed", "Could not create user."));
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(donorProfileRepository, messagePublisher, identityProvider, unitOfWork);
        var command = new RegisterDonorCommandBuilder().Build();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IdentityProvider.CreateUserFailed");
        donorProfileRepository.DonorProfiles.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    private static RegisterDonorCommandHandler CreateHandler(
        InMemoryDonorProfileRepository donorProfileRepository,
        FakeMessagePublisher messagePublisher,
        FakeIdentityProvider identityProvider,
        FakeUnitOfWork unitOfWork)
    {
        return new RegisterDonorCommandHandler(
            donorProfileRepository,
            messagePublisher,
            identityProvider,
            unitOfWork,
            NullLogger<RegisterDonorCommandHandler>.Instance);
    }
}
