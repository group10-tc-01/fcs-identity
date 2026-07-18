namespace Fcs.Identity.Application.Abstractions.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(string topicName, TMessage message, CancellationToken cancellationToken = default);
}
