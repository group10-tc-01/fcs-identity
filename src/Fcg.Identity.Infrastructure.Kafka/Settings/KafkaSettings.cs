using System.ComponentModel.DataAnnotations;

namespace Fcg.Identity.Infrastructure.Kafka.Settings;

public sealed class KafkaSettings
{
    public const string SectionName = "KafkaSettings";

    [Required]
    public string BootstrapServers { get; set; } = "localhost:9092";

    [Required]
    public string TopicName { get; set; } = "identity-events";
}
