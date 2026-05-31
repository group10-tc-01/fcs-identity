namespace Fcg.Identity.IntegratedTests.Support;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DockerAvailableFactAttribute : FactAttribute
{
    public DockerAvailableFactAttribute()
    {
        if (!IsDockerAvailable())
        {
            Skip = "Docker is required to run this Testcontainers integration test.";
        }
    }

    private static bool IsDockerAvailable()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("RUN_TESTCONTAINERS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")))
        {
            return true;
        }

        return !OperatingSystem.IsWindows() && File.Exists("/var/run/docker.sock");
    }
}
