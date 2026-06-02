namespace Fcg.Identity.Application.Audit;

public sealed record AuditActor(Guid? ActorId, string? ActorType);
