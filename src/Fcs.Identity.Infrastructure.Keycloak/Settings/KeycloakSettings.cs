using System.Diagnostics.CodeAnalysis;

namespace Fcs.Identity.Infrastructure.Keycloak.Settings;

[ExcludeFromCodeCoverage]
public sealed class KeycloakSettings
{
    public const string SectionName = "Keycloak";

    public string BaseUrl { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Realm { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string AdminRealm { get; set; } = "master";

    public string AdminClientId { get; set; } = "admin-cli";

    public string AdminUsername { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;

    public KeycloakRetrySettings Retry { get; set; } = new();
}

[ExcludeFromCodeCoverage]
public sealed class KeycloakRetrySettings
{
    public int RetryCount { get; set; } = 3;

    public int BaseDelayMilliseconds { get; set; } = 200;
}
