namespace Fcg.Identity.WebApi.Observability;

public sealed class ObservabilitySettings
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "Fcg.Identity";
    public string OtlpEndpoint { get; set; } = string.Empty;
    public string OtlpAuthHeader { get; set; } = string.Empty;
    public bool EnableOtlpExporter { get; set; }
}
