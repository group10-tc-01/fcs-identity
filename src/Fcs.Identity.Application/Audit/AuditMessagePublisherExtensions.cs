using Fcs.Identity.Application.Abstractions.Messaging;
using Fcs.Identity.Application.IntegrationEvents.AuditLogs;

namespace Fcs.Identity.Application.Audit;

public static class AuditMessagePublisherExtensions
{
    public static void PublishAuditLogFireAndForget(this IMessagePublisher messagePublisher, AuditLogRequestedEvent auditEvent)
    {
        _ = Task.Run(async () =>
        {
            await messagePublisher.PublishAsync(KafkaTopicKeys.AuditLog, auditEvent, CancellationToken.None);
        }, CancellationToken.None);
    }
}
