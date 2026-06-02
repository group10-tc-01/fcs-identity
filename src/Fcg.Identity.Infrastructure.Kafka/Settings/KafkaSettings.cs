using System.Diagnostics.CodeAnalysis;

namespace Fcg.Identity.Infrastructure.Kafka.Settings;

[ExcludeFromCodeCoverage]
public sealed class KafkaSettings
{
    public const string SectionName = "KafkaSettings";

    public string BootstrapServers { get; set; }

    public string TopicName { get; set; }
}
