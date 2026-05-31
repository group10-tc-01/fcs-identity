using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Confluent.Kafka;
using Fcg.Identity.Application.Abstractions.Messaging;
using Fcg.Identity.Infrastructure.Kafka.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fcg.Identity.Infrastructure.Kafka.Messaging;

[ExcludeFromCodeCoverage]
public sealed class KafkaMessagePublisher : IMessagePublisher
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaMessagePublisher> _logger;

    public KafkaMessagePublisher(IOptions<KafkaSettings> options, ILogger<KafkaMessagePublisher> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            Acks = Acks.All
        };

        using var producer = new ProducerBuilder<Null, string>(config).Build();
        var payload = JsonSerializer.Serialize(message);
        await producer.ProduceAsync(_settings.TopicName, new Message<Null, string> { Value = payload }, cancellationToken);
        _logger.LogInformation("Published message to topic {TopicName}", _settings.TopicName);
    }
}
