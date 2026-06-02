using Fcg.Identity.Application.Abstractions.Authentication;
using Fcg.Identity.Application.Abstractions.Messaging;
using Fcg.Identity.Application.Audit;
using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Domain.ManagerProfiles;
using Fcg.Identity.Domain.Shared;
using Fcg.Identity.Domain.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Fcg.Identity.Application.UseCases.Profiles.GetMe;

public sealed class GetMeQueryHandler : IQueryHandler<GetMeQuery, GetMeResponse>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDonorProfileRepository _donorProfileRepository;
    private readonly IManagerProfileRepository _managerProfileRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<GetMeQueryHandler> _logger;

    public GetMeQueryHandler(
        ICurrentUser currentUser,
        IDonorProfileRepository donorProfileRepository,
        IManagerProfileRepository managerProfileRepository,
        IMessagePublisher messagePublisher,
        ILogger<GetMeQueryHandler> logger)
    {
        _currentUser = currentUser;
        _donorProfileRepository = donorProfileRepository;
        _managerProfileRepository = managerProfileRepository;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<Result<GetMeResponse>> Handle(GetMeQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Get current profile flow started. IsAuthenticated: {IsAuthenticated}. KeycloakUserId: {KeycloakUserId}. Roles: {Roles}",
            _currentUser.IsAuthenticated,
            _currentUser.KeycloakUserId,
            _currentUser.Roles);

        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.KeycloakUserId))
        {
            _logger.LogWarning("Get current profile flow stopped because user is not authenticated");
            return Error.Unauthorized("CurrentUser.Unauthenticated", "User is not authenticated.");
        }

        if (_currentUser.Roles.Contains(IdentityRoles.Donor))
        {
            _logger.LogInformation("Resolving donor profile by KeycloakUserId {KeycloakUserId}", _currentUser.KeycloakUserId);

            var donorProfile = await _donorProfileRepository.GetByKeycloakUserIdAsync(_currentUser.KeycloakUserId, cancellationToken);
            if (donorProfile is null)
            {
                _logger.LogWarning("Donor profile not found for KeycloakUserId {KeycloakUserId}", _currentUser.KeycloakUserId);
                return Error.NotFound("Profile.NotFound", "Profile was not found.");
            }

            PublishProfileViewedAudit(nameof(DonorProfile), donorProfile.Id, IdentityRoles.Donor);

            _logger.LogInformation(
                "Get current profile flow completed for donor. DonorProfileId: {DonorProfileId}",
                donorProfile.Id);

            return new GetMeResponse(
                donorProfile.Id,
                donorProfile.KeycloakUserId,
                donorProfile.FullName,
                donorProfile.Email.Value,
                IdentityRoles.Donor);
        }

        if (_currentUser.Roles.Contains(IdentityRoles.Manager))
        {
            _logger.LogInformation("Resolving manager profile by KeycloakUserId {KeycloakUserId}", _currentUser.KeycloakUserId);

            var managerProfile = await _managerProfileRepository.GetByKeycloakUserIdAsync(_currentUser.KeycloakUserId, cancellationToken);
            if (managerProfile is null)
            {
                _logger.LogWarning("Manager profile not found for KeycloakUserId {KeycloakUserId}", _currentUser.KeycloakUserId);
                return Error.NotFound("Profile.NotFound", "Profile was not found.");
            }

            PublishProfileViewedAudit(nameof(ManagerProfile), managerProfile.Id, IdentityRoles.Manager);

            _logger.LogInformation(
                "Get current profile flow completed for manager. ManagerProfileId: {ManagerProfileId}",
                managerProfile.Id);

            return new GetMeResponse(
                managerProfile.Id,
                managerProfile.KeycloakUserId,
                managerProfile.FullName,
                managerProfile.Email.Value,
                IdentityRoles.Manager);
        }

        _logger.LogWarning(
            "Get current profile flow stopped because roles are not allowed. Roles: {Roles}",
            _currentUser.Roles);

        return Error.Unauthorized("CurrentUser.RoleNotAllowed", "User role is not allowed.");
    }

    private void PublishProfileViewedAudit(string entityName, Guid profileId, string actorType)
    {
        _logger.LogInformation(
            "Publishing profile viewed audit log. EntityName: {EntityName}. ProfileId: {ProfileId}. ActorType: {ActorType}",
            entityName,
            profileId,
            actorType);

        _messagePublisher.PublishAuditLogFireAndForget(
            AuditLogRequestedEvent.Create(
                AuditActions.ProfileViewed,
                entityName,
                profileId,
                actorType,
                profileId.ToString()));
    }
}
