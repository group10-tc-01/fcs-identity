using System.Text.Json;
using FluentAssertions;

namespace Fcs.Identity.UnitTests.Configuration;

public sealed class KeycloakRealmConfigurationTests
{
    [Fact]
    public void Given_RealmConfiguration_When_SolidarityApiIsLoaded_Then_ShouldExposeEmailClaimInTokens()
    {
        // Arrange
        var realmPath = Path.Combine(AppContext.BaseDirectory, "keycloak", "conexao-solidaria-realm.json");
        using var document = JsonDocument.Parse(File.ReadAllText(realmPath));

        // Act
        var client = document.RootElement.GetProperty("clients")
            .EnumerateArray()
            .Single(item => item.GetProperty("clientId").GetString() == "solidarity-api");
        var emailMapper = client.GetProperty("protocolMappers")
            .EnumerateArray()
            .Single(mapper => mapper.GetProperty("name").GetString() == "email");
        var configuration = emailMapper.GetProperty("config");

        // Assert
        emailMapper.GetProperty("protocolMapper").GetString().Should().Be("oidc-usermodel-property-mapper");
        configuration.GetProperty("user.attribute").GetString().Should().Be("email");
        configuration.GetProperty("claim.name").GetString().Should().Be("email");
        configuration.GetProperty("access.token.claim").GetString().Should().Be("true");
        configuration.GetProperty("id.token.claim").GetString().Should().Be("true");
        configuration.GetProperty("userinfo.token.claim").GetString().Should().Be("true");
    }
}
