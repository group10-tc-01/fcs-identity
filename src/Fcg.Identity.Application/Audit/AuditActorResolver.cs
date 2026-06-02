using Fcg.Identity.Application.Abstractions.Identity;
using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Domain.ManagerProfiles;
using Fcg.Identity.Domain.Shared;

namespace Fcg.Identity.Application.Audit;

public static class AuditActorResolver
{
    public static async Task<AuditActor> ResolveAsync(
        LoginIdentityUserResponse token,
        IDonorProfileRepository donorProfileRepository,
        IManagerProfileRepository managerProfileRepository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token.KeycloakUserId))
        {
            return new AuditActor(null, "Public");
        }

        var roles = token.Roles ?? [];

        if (roles.Contains(IdentityRoles.Donor))
        {
            var donorProfile = await donorProfileRepository.GetByKeycloakUserIdAsync(token.KeycloakUserId, cancellationToken);
            return new AuditActor(donorProfile?.Id, IdentityRoles.Donor);
        }

        if (roles.Contains(IdentityRoles.Manager))
        {
            var managerProfile = await managerProfileRepository.GetByKeycloakUserIdAsync(token.KeycloakUserId, cancellationToken);
            return new AuditActor(managerProfile?.Id, IdentityRoles.Manager);
        }

        return new AuditActor(null, "Public");
    }
}
