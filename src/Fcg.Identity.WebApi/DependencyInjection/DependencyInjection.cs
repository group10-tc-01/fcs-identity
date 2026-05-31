using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Fcg.Identity.Application.Abstractions.Authentication;
using Fcg.Identity.Infrastructure.SqlServer.Persistence;
using Fcg.Identity.WebApi.Authentication;
using Fcg.Identity.WebApi.Filters;
using Fcg.Identity.WebApi.Observability;
using Fcg.Identity.WebApi.Swagger;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
namespace Fcg.Identity.WebApi.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
               .AddJsonOptions(options =>
               {
                   options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
               });

        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSwaggerConfiguration(configuration);

        services.AddVersioning();
        services.AddFilters();
        services.AddHealthChecks().AddDbContextCheck<FcgIdentityDbContext>();
        services.AddRouting(options => options.LowercaseUrls = true);
        services.AddObservabilitySettings(configuration);
        services.AddObservability(configuration);
        services.AddSerilogLogging(configuration);
        return services;
    }

    private static void AddSwaggerConfiguration(this IServiceCollection services, IConfiguration configuration) => services.AddIdentitySwagger();

    private static void AddVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
    }

    private static void AddFilters(this IServiceCollection services)
    {
        services.AddMvc(options =>
        {
            options.Filters.Add<TrimStringsActionFilter>();
        });
    }

    private static void AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = GetObservabilitySettings(configuration);

        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";

        var resourceBuilder = ObservabilityTelemetry.CreateResourceBuilder(settings, environment);

        services.AddOpenTelemetry()
            .WithTracing(builder => builder.ConfigureTracing(settings, resourceBuilder))
            .WithMetrics(builder => builder.ConfigureMetrics(settings, resourceBuilder));
    }

    private static void AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = GetObservabilitySettings(configuration);

        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", "FCG.Identity")
            .Enrich.WithProperty("Environment", environment)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");

        if (settings.EnableOtlpExporter && !string.IsNullOrEmpty(settings.OtlpEndpoint))
        {
            loggerConfig.WriteTo.OpenTelemetry(otlpOptions =>
            {
                otlpOptions.Endpoint = $"{settings.OtlpEndpoint}/otlp/v1/logs";
                otlpOptions.Protocol = OtlpProtocol.HttpProtobuf;
                otlpOptions.Headers = new Dictionary<string, string>
                {
                    ["Authorization"] = settings.OtlpAuthHeader
                };
                otlpOptions.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = settings.ServiceName,
                    ["deployment.environment"] = environment
                };
            });
        }

        Log.Logger = loggerConfig.CreateLogger();

        Log.Information("Starting {Application} application", "FCG.Identity");
        Log.Information("Environment: {Environment}", environment);

        if (settings.EnableOtlpExporter)
        {
            Log.Information("OTLP exporter enabled — sending telemetry to {Endpoint}", settings.OtlpEndpoint);
        }
        else
        {
            Log.Information("OTLP exporter disabled — telemetry is console-only");
        }

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });
    }

    private static void AddObservabilitySettings(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<ObservabilitySettings>()
            .Bind(configuration.GetRequiredSection(ObservabilitySettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static ObservabilitySettings GetObservabilitySettings(IConfiguration configuration)
    {
        return configuration
            .GetRequiredSection(ObservabilitySettings.SectionName)
            .Get<ObservabilitySettings>()
            ?? throw new InvalidOperationException("Observability settings must be configured.");
    }
}
