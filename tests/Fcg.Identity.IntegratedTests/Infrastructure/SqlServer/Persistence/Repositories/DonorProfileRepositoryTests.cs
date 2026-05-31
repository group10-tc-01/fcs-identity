using Fcg.Identity.CommomTestsUtilities.Builders.DonorProfiles;
using Fcg.Identity.Domain.Abstractions;
using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.IntegratedTests.Configurations;
using Fcg.Identity.IntegratedTests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Identity.IntegratedTests.Infrastructure.SqlServer.Persistence.Repositories;

[Collection(IntegrationTestCollection.Name)]
public sealed class DonorProfileRepositoryTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public DonorProfileRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
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
    public async Task Given_AddAsync_Called_When_DonorProfileIsSaved_Then_ShouldReturnById()
    {
        // Arrange
        var donorProfile = new DonorProfileBuilder().Build();
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDonorProfileRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Act
        await repository.AddAsync(donorProfile, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        var result = await repository.GetByIdAsync(donorProfile.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(donorProfile.Id);
        result.Email.Value.Should().Be(donorProfile.Email.Value);
        result.Cpf.Value.Should().Be(donorProfile.Cpf.Value);
    }

    [DockerAvailableFact]
    public async Task Given_GetByKeycloakUserIdAsync_Called_When_DonorProfileExists_Then_ShouldReturnDonorProfile()
    {
        // Arrange
        var donorProfile = new DonorProfileBuilder().Build();
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDonorProfileRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repository.AddAsync(donorProfile, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await repository.GetByKeycloakUserIdAsync(donorProfile.KeycloakUserId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.KeycloakUserId.Should().Be(donorProfile.KeycloakUserId);
    }

    [DockerAvailableFact]
    public async Task Given_ExistsByEmailAsync_Called_When_EmailExists_Then_ShouldReturnTrue()
    {
        // Arrange
        var donorProfile = new DonorProfileBuilder().Build();
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDonorProfileRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repository.AddAsync(donorProfile, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await repository.ExistsByEmailAsync(donorProfile.Email.Value, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [DockerAvailableFact]
    public async Task Given_ExistsByEmailAsync_Called_When_EmailDoesNotExist_Then_ShouldReturnFalse()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDonorProfileRepository>();

        // Act
        var result = await repository.ExistsByEmailAsync("missing@email.com", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task Given_ExistsByEmailAsync_Called_When_EmailIsInvalid_Then_ShouldReturnFalse()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDonorProfileRepository>();

        // Act
        var result = await repository.ExistsByEmailAsync("invalid-email", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task Given_ExistsByCpfAsync_Called_When_CpfExists_Then_ShouldReturnTrue()
    {
        // Arrange
        var donorProfile = new DonorProfileBuilder().Build();
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDonorProfileRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repository.AddAsync(donorProfile, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await repository.ExistsByCpfAsync(donorProfile.Cpf.Value, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [DockerAvailableFact]
    public async Task Given_ExistsByCpfAsync_Called_When_CpfDoesNotExist_Then_ShouldReturnFalse()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDonorProfileRepository>();

        // Act
        var result = await repository.ExistsByCpfAsync("39053344705", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task Given_ExistsByCpfAsync_Called_When_CpfIsInvalid_Then_ShouldReturnFalse()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDonorProfileRepository>();

        // Act
        var result = await repository.ExistsByCpfAsync("11111111111", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}
