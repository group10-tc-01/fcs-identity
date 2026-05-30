using Fcg.Identity.CommomTestsUtilities.Builders.DonorProfiles;
using Fcg.Identity.CommomTestsUtilities.Fakers.Shared;
using Fcg.Identity.Domain.DonorProfiles;
using FluentAssertions;

namespace Fcg.Identity.UnitTests.Domain.DonorProfiles;

public sealed class DonorProfileTests
{
    [Fact]
    public void Given_Build_Called_When_DonorProfileBuilderHasDefaultData_Then_ShouldCreateValidDonorProfile()
    {
        // Arrange
        var builder = new DonorProfileBuilder();

        // Act
        var donorProfile = builder.Build();

        // Assert
        donorProfile.Id.Should().NotBeEmpty();
        donorProfile.KeycloakUserId.Should().NotBeNullOrWhiteSpace();
        donorProfile.FullName.Should().NotBeNullOrWhiteSpace();
        donorProfile.Email.Value.Should().NotBeNullOrWhiteSpace();
        donorProfile.Cpf.Value.Should().HaveLength(11);
        donorProfile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        donorProfile.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Given_Create_Called_When_KeycloakUserIdIsEmpty_Then_ShouldReturnFailure()
    {
        // Arrange
        var builder = new DonorProfileBuilder()
            .WithKeycloakUserId(string.Empty);

        // Act
        var result = builder.BuildResult();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DonorProfile.KeycloakUserIdRequired");
    }

    [Fact]
    public void Given_Create_Called_When_FullNameIsEmpty_Then_ShouldReturnFailure()
    {
        // Arrange
        var builder = new DonorProfileBuilder()
            .WithFullName(string.Empty);

        // Act
        var result = builder.BuildResult();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DonorProfile.FullNameRequired");
    }

    [Fact]
    public void Given_Create_Called_When_EmailIsEmpty_Then_ShouldReturnFailure()
    {
        // Arrange
        var builder = new DonorProfileBuilder()
            .WithEmail(string.Empty);

        // Act
        var result = builder.BuildResult();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DonorProfile.EmailRequired");
    }

    [Fact]
    public void Given_Create_Called_When_CpfIsEmpty_Then_ShouldReturnFailure()
    {
        // Arrange
        var builder = new DonorProfileBuilder()
            .WithCpf(string.Empty);

        // Act
        var result = builder.BuildResult();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DonorProfile.CpfRequired");
    }

    [Fact]
    public void Given_Create_Called_When_ProfileDataHasExtraSpaces_Then_ShouldNormalizeProfileData()
    {
        // Arrange
        const string keycloakUserId = " keycloak-user-id ";
        const string fullName = " Maria Silva ";
        const string email = " MARIA@EMAIL.COM ";
        var cpf = $" {CpfFaker.Generate()} ";

        // Act
        var result = DonorProfile.Create(keycloakUserId, fullName, email, cpf);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.KeycloakUserId.Should().Be("keycloak-user-id");
        result.Value.FullName.Should().Be("Maria Silva");
        result.Value.Email.Value.Should().Be("maria@email.com");
        result.Value.Cpf.Value.Should().Be(cpf.Trim());
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Given_Create_Called_When_EmailIsInvalid_Then_ShouldReturnFailure()
    {
        // Arrange
        var builder = new DonorProfileBuilder()
            .WithEmail("invalid-email");

        // Act
        var result = builder.BuildResult();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Invalid");
    }

    [Fact]
    public void Given_Create_Called_When_CpfIsInvalid_Then_ShouldReturnFailure()
    {
        // Arrange
        var builder = new DonorProfileBuilder()
            .WithCpf("11111111111");

        // Act
        var result = builder.BuildResult();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Cpf.Invalid");
    }
}
