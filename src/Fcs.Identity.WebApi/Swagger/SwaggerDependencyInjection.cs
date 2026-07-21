using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;

namespace Fcs.Identity.WebApi.Swagger;

[ExcludeFromCodeCoverage]
public static class SwaggerDependencyInjection
{
    public static IServiceCollection AddIdentitySwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Fcs.Identity API",
                Version = "v1.0",
                Description = BuildDeploymentDescription(configuration),
            });

            options.AddSecurityDefinition(SwaggerConstants.BearerSecurityScheme, new OpenApiSecurityScheme
            {
                Description = "JWT Bearer token issued by Keycloak. Use: Bearer {accessToken}.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.OperationFilter<SwaggerAuthorizationOperationFilter>();
            options.OperationFilter<SwaggerEndpointDocumentationOperationFilter>();
        });

        return services;
    }

    private static string BuildDeploymentDescription(IConfiguration configuration)
    {
        var deployedAt = configuration["Deployment:DeployedAt"] ?? "não informado";
        var sourceSha = configuration["Deployment:SourceSha"] ?? "não informado";
        var image = configuration["Deployment:Image"] ?? "não informado";

        return $"""
            API de identidade e acesso da plataforma Conexão Solidária.

            ### Versão

            **Data/hora (UTC)**

            {deployedAt}

            **Commit**

            {sourceSha}

            **Imagem**

            {image}
            """;
    }
}
