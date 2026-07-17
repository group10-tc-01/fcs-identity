namespace Fcs.Identity.Application.IntegrationEvents.AuditLogs;

public sealed record AuditLogRequestedEvent(
    Guid EventId,
    DateTime OccurredAt,
    string ServiceName,
    string Action,
    string EntityName,
    string? EntityId,
    Guid? ActorId,
    string? ActorType,
    string? CorrelationId = null,
    string? IpAddress = null,
    string? UserAgent = null,
    IReadOnlyDictionary<string, object?>? Metadata = null)
{
    private const string IdentityServiceName = "fcs-identity";

    public static AuditLogRequestedEvent Create(
        string action,
        string entityName,
        Guid? actorId = null,
        string? actorType = null,
        string? entityId = null,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        return new AuditLogRequestedEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            IdentityServiceName,
            action,
            entityName,
            entityId,
            actorId,
            actorType,
            correlationId,
            ipAddress,
            userAgent,
            metadata);
    }
}
