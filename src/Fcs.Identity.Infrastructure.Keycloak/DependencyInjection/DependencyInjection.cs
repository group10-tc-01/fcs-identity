using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using Fcs.Identity.Application.Abstractions.Identity;
using Fcs.Identity.Infrastructure.Keycloak.Http;
using Fcs.Identity.Infrastructure.Keycloak.Identity;
using Fcs.Identity.Infrastructure.Keycloak.Settings;
using Keycloak.AuthServices.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Refit;

namespace Fcs.Identity.Infrastructure.Keycloak.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    private const string AccessTokenCookieName = "fcs_access_token";

    public static IServiceCollection AddKeycloakInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKeycloakOptions(configuration);
        services.AddKeycloakAuthentication(configuration);
        services.AddAuthorization();
        services.AddKeycloakHttpClient();
        services.AddKeycloakServices();

        return services;
    }

    private static IServiceCollection AddKeycloakOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<KeycloakSettings>()
            .Bind(configuration.GetRequiredSection(KeycloakSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration
            .GetRequiredSection(KeycloakSettings.SectionName)
            .Get<KeycloakSettings>()
            ?? throw new InvalidOperationException("Keycloak settings must be configured.");
        var internalIssuer = $"{settings.BaseUrl.TrimEnd('/')}/realms/{settings.Realm}";
        var issuer = string.IsNullOrWhiteSpace(settings.Issuer) ? internalIssuer : settings.Issuer;

        services.AddKeycloakWebApiAuthentication(
            keycloakOptions =>
            {
                keycloakOptions.AuthServerUrl = settings.BaseUrl;
                keycloakOptions.Realm = settings.Realm;
                keycloakOptions.Resource = settings.ClientId;
                keycloakOptions.Audience = settings.ClientId;
                keycloakOptions.VerifyTokenAudience = false;
                keycloakOptions.SslRequired = "none";
                keycloakOptions.NameClaimType = "preferred_username";
                keycloakOptions.RoleClaimType = ClaimTypes.Role;
            },
            jwtBearerOptions =>
            {
                jwtBearerOptions.RequireHttpsMetadata = false;
                jwtBearerOptions.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Token)
                            && context.Request.Cookies.TryGetValue(AccessTokenCookieName, out var accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
                jwtBearerOptions.TokenValidationParameters.ValidateAudience = false;
                jwtBearerOptions.TokenValidationParameters.NameClaimType = "preferred_username";
                jwtBearerOptions.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                jwtBearerOptions.TokenValidationParameters.ValidIssuer = null;
                jwtBearerOptions.TokenValidationParameters.ValidIssuers = [internalIssuer, issuer];
            });

        return services;
    }

    private static IServiceCollection AddKeycloakHttpClient(this IServiceCollection services)
    {
        services.AddRefitClient<IKeycloakApi>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<KeycloakSettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
            })
            .AddPolicyHandler((serviceProvider, _) =>
            {
                var retry = serviceProvider.GetRequiredService<IOptions<KeycloakSettings>>().Value.Retry;
                return CreateKeycloakRetryPolicy(retry);
            });

        return services;
    }

    private static IServiceCollection AddKeycloakServices(this IServiceCollection services)
    {
        services.AddScoped<IIdentityProvider, KeycloakIdentityProvider>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateKeycloakRetryPolicy(KeycloakRetrySettings retry)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retry.RetryCount,
                attempt => TimeSpan.FromMilliseconds(retry.BaseDelayMilliseconds * Math.Pow(2, attempt - 1)));
    }
}
