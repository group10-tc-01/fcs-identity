using Fcg.Identity.Application.Abstractions.Messaging;

namespace Fcg.Identity.CommomTestsUtilities.TestDoubles;

public sealed class FakeMessagePublisher : IMessagePublisher
{
    private readonly List<object> _publishedMessages = [];

    public IReadOnlyCollection<object> PublishedMessages
    {
        get
        {
            lock (_publishedMessages)
            {
                return _publishedMessages.ToArray();
            }
        }
    }

    public void Reset()
    {
        lock (_publishedMessages)
        {
            _publishedMessages.Clear();
        }
    }

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
    {
        lock (_publishedMessages)
        {
            _publishedMessages.Add(message!);
        }

        return Task.CompletedTask;
    }

    public async Task<TMessage> WaitForSingleMessageAsync<TMessage>()
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while (DateTime.UtcNow < deadline)
        {
            var message = PublishedMessages.SingleOrDefault();

            if (message is TMessage typedMessage)
            {
                return typedMessage;
            }

            if (message is not null)
            {
                throw new InvalidOperationException($"Expected message of type {typeof(TMessage).Name}, but found {message.GetType().Name}.");
            }

            await Task.Delay(10);
        }

        var publishedMessage = PublishedMessages.SingleOrDefault()
            ?? throw new InvalidOperationException($"Expected one message of type {typeof(TMessage).Name}, but no message was published.");

        return publishedMessage is TMessage typedPublishedMessage
            ? typedPublishedMessage
            : throw new InvalidOperationException($"Expected message of type {typeof(TMessage).Name}, but found {publishedMessage.GetType().Name}.");
    }
}
