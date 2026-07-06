using System.Xml.Linq;
using FluentAssertions;

namespace Fcs.Identity.ArchitectureTests.Architecture;

public class ProjectStructureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    public static TheoryData<string> ProductionProjects => new()
    {
        "src/Fcs.Identity.Application/Fcs.Identity.Application.csproj",
        "src/Fcs.Identity.Domain/Fcs.Identity.Domain.csproj",
        "src/Fcs.Identity.Resources/Fcs.Identity.Resources.csproj",
        "src/Fcs.Identity.Infrastructure.Kafka/Fcs.Identity.Infrastructure.Kafka.csproj",
        "src/Fcs.Identity.Infrastructure.Keycloak/Fcs.Identity.Infrastructure.Keycloak.csproj",
        "src/Fcs.Identity.Infrastructure.SqlServer/Fcs.Identity.Infrastructure.SqlServer.csproj",
        "src/Fcs.Identity.WebApi/Fcs.Identity.WebApi.csproj"
    };

    [Theory]
    [MemberData(nameof(ProductionProjects))]
    public void Given_ProductionProject_When_ProjectFileIsValidated_Then_ShouldTargetNet10(string relativeProjectPath)
    {
        // Arrange
        var projectPath = Path.Combine(RepositoryRoot, relativeProjectPath);

        // Act
        var targetFramework = GetTargetFramework(projectPath);

        // Assert
        targetFramework.Should().Be("net10.0", "Fase 05 backend services are standardized on .NET 10");
    }

    [Fact]
    public void Given_SourceProjects_When_ProjectStructureIsValidated_Then_ShouldFollowFcsIdentityStructure()
    {
        // Arrange
        var expectedProjectDirectories = new[]
        {
            "src/Fcs.Identity.Domain",
            "src/Fcs.Identity.Resources",
            "src/Fcs.Identity.Application",
            "src/Fcs.Identity.Infrastructure.Kafka",
            "src/Fcs.Identity.Infrastructure.Keycloak",
            "src/Fcs.Identity.Infrastructure.SqlServer",
            "src/Fcs.Identity.WebApi"
        };

        // Act
        var projectDirectories = expectedProjectDirectories
            .Select(path => Path.Combine(RepositoryRoot, path))
            .ToArray();

        // Assert
        projectDirectories
            .Should()
            .OnlyContain(path => Directory.Exists(path), "fcs-identity must preserve the phase 04 clean architecture project layout");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Fcs.Identity.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find fcs-identity repository root.");
    }

    private static string? GetTargetFramework(string projectPath)
    {
        return GetTargetFrameworkFrom(projectPath)
               ?? GetTargetFrameworkFrom(Path.Combine(RepositoryRoot, "Directory.Build.props"));
    }

    private static string? GetTargetFrameworkFrom(string path)
    {
        var project = XDocument.Load(path);

        return project
            .Root?
            .Elements("PropertyGroup")
            .Elements("TargetFramework")
            .Select(element => element.Value)
            .FirstOrDefault();
    }
}
