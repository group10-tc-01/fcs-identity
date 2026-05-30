using Fcg.Identity.Infrastructure.Keycloak.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Identity.Infrastructure.Keycloak.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddKeycloakInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<KeycloakSettings>()
            .Bind(configuration.GetRequiredSection(KeycloakSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
