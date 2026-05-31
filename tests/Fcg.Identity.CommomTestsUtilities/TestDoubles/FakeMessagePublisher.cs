using Fcg.Identity.Application.Abstractions.Messaging;

namespace Fcg.Identity.CommomTestsUtilities.TestDoubles;

public sealed class FakeMessagePublisher : IMessagePublisher
{
    public List<object> PublishedMessages { get; } = new();

    public void Reset()
    {
        PublishedMessages.Clear();
    }

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
    {
        PublishedMessages.Add(message!);
        return Task.CompletedTask;
    }
}
