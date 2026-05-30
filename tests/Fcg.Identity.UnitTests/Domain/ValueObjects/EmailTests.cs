using Fcg.Identity.CommomTestsUtilities.Fakers.Shared;
using Fcg.Identity.Domain.Shared.ValueObjects;
using FluentAssertions;

namespace Fcg.Identity.UnitTests.Domain.ValueObjects;

public sealed class EmailTests
{
    [Fact]
    public void Given_Create_Called_When_EmailHasExtraSpacesAndUppercaseLetters_Then_ShouldNormalizeEmail()
    {
        // Arrange
        var generatedEmail = EmailFaker.Generate();
        var email = $" {generatedEmail.ToUpperInvariant()} ";

        // Act
        var result = Email.Create(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(generatedEmail);
    }

    [Fact]
    public void Given_Create_Called_When_EmailIsInvalid_Then_ShouldReturnFailure()
    {
        // Arrange
        const string email = "maria-email.com";

        // Act
        var result = Email.Create(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Invalid");
    }
}
