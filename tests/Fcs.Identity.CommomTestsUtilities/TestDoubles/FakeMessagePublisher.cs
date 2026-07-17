using Fcs.Identity.Application.Abstractions.Messaging;

namespace Fcs.Identity.CommomTestsUtilities.TestDoubles;

public sealed class FakeMessagePublisher : IMessagePublisher
{
    private readonly List<object> _publishedMessages = [];
    private readonly List<string> _publishedTopicNames = [];

    public Exception? ExceptionToThrow { get; private set; }

    public IReadOnlyCollection<string> PublishedTopicNames
    {
        get
        {
            lock (_publishedTopicNames)
            {
                return _publishedTopicNames.ToArray();
            }
        }
    }

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

        lock (_publishedTopicNames)
        {
            _publishedTopicNames.Clear();
        }
    }

    public void ConfigureFailure(Exception exception) => ExceptionToThrow = exception;

    public Task PublishAsync<TMessage>(string topicName, TMessage message, CancellationToken cancellationToken = default)
    {
        return PublishCoreAsync(topicName, message);
    }

    private Task PublishCoreAsync<TMessage>(string topicName, TMessage message)
    {
        if (ExceptionToThrow is not null)
        {
            return Task.FromException(ExceptionToThrow);
        }

        lock (_publishedMessages)
        {
            _publishedMessages.Add(message!);
        }

        lock (_publishedTopicNames)
        {
            _publishedTopicNames.Add(topicName);
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

    public async Task<TMessage> WaitForMessageAsync<TMessage>()
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while (DateTime.UtcNow < deadline)
        {
            var message = PublishedMessages.OfType<TMessage>().SingleOrDefault();
            if (message is not null)
            {
                return message;
            }

            await Task.Delay(10);
        }

        throw new InvalidOperationException($"Expected one message of type {typeof(TMessage).Name}, but no message was published.");
    }
}
