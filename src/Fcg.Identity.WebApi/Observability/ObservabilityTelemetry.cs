using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Fcg.Identity.WebApi.Observability;

[ExcludeFromCodeCoverage]
public static class ObservabilityTelemetry
{
    public static ResourceBuilder CreateResourceBuilder(ObservabilitySettings settings, string environment)
    {
        return ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: settings.ServiceName,
                serviceVersion: typeof(ObservabilityTelemetry).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                serviceNamespace: "FCG")
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment
            });
    }

    public static TracerProviderBuilder ConfigureTracing(
        this TracerProviderBuilder builder,
        ObservabilitySettings settings,
        ResourceBuilder resourceBuilder)
    {
        builder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.Filter = httpContext =>
                    !httpContext.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation();

        if (settings.EnableOtlpExporter)
        {
            builder.AddOtlpExporter(exporterOpts =>
            {
                exporterOpts.Endpoint = new Uri($"{settings.OtlpEndpoint}/otlp/v1/traces");
                exporterOpts.Protocol = OtlpExportProtocol.HttpProtobuf;
                exporterOpts.Headers = $"Authorization={settings.OtlpAuthHeader}";
            });
        }

        return builder;
    }

    public static MeterProviderBuilder ConfigureMetrics(
        this MeterProviderBuilder builder,
        ObservabilitySettings settings,
        ResourceBuilder resourceBuilder)
    {
        builder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (settings.EnableOtlpExporter)
        {
            builder.AddOtlpExporter(exporterOpts =>
            {
                exporterOpts.Endpoint = new Uri($"{settings.OtlpEndpoint}/otlp/v1/metrics");
                exporterOpts.Protocol = OtlpExportProtocol.HttpProtobuf;
                exporterOpts.Headers = $"Authorization={settings.OtlpAuthHeader}";
            });
        }

        return builder;
    }
}
