using System.ComponentModel.DataAnnotations;

namespace Fcg.Identity.Infrastructure.Keycloak.Settings;

public sealed class KeycloakSettings
{
    public const string SectionName = "Keycloak";

    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string Realm { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;
}
