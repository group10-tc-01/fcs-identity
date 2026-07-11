using System.Diagnostics.CodeAnalysis;
using Fcs.Identity.WebApi.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fcs.Identity.WebApi.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class PipelineDependencyInjection
{
    public static WebApplication UseWebApiPipeline(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Application started successfully");
        logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

        var applyMigrations = app.Environment.IsDevelopment()
            || app.Environment.EnvironmentName == "Docker"
            || app.Configuration.GetValue<bool>("Database:ApplyMigrations");

        if (applyMigrations)
        {
            app.ApplyMigrations();
            logger.LogInformation("Migrations applied");
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseGlobalCorrelationId();
        app.UseRequestFlowLogging();
        app.UseCustomerExceptionHandler();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            }

        });
        app.MapPrometheusScrapingEndpoint("/metrics");

        var enableHttpsRedirection = app.Configuration.GetValue("HttpsRedirection:Enabled", true);
        if (!app.Environment.IsDevelopment()
            && app.Environment.EnvironmentName != "Docker"
            && enableHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }

}
