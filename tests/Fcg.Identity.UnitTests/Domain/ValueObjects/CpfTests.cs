using Fcg.Identity.CommomTestsUtilities.Fakers.Shared;
using Fcg.Identity.Domain.Shared.ValueObjects;
using FluentAssertions;

namespace Fcg.Identity.UnitTests.Domain.ValueObjects;

public sealed class CpfTests
{
    [Fact]
    public void Given_Create_Called_When_CpfIsMasked_Then_ShouldNormalizeCpf()
    {
        // Arrange
        var cpf = CpfFaker.GenerateMasked();
        var normalizedCpf = cpf.Replace(".", string.Empty).Replace("-", string.Empty);

        // Act
        var result = Cpf.Create(cpf);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(normalizedCpf);
    }

    [Fact]
    public void Given_Masked_Accessed_When_CpfIsValid_Then_ShouldReturnMaskedCpf()
    {
        // Arrange
        var cpf = CpfFaker.Generate();
        var expectedMaskedCpf = $"{cpf[..3]}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf[9..]}";
        var result = Cpf.Create(cpf);

        // Act
        var maskedCpf = result.Value.Masked;

        // Assert
        result.IsSuccess.Should().BeTrue();
        maskedCpf.Should().Be(expectedMaskedCpf);
    }

    [Fact]
    public void Given_Create_Called_When_CpfIsInvalid_Then_ShouldReturnFailure()
    {
        // Arrange
        const string cpf = "111.111.111-11";

        // Act
        var result = Cpf.Create(cpf);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Cpf.Invalid");
    }
}
