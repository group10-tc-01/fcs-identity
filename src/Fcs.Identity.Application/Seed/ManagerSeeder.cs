using Fcs.Identity.Application.Abstractions.Identity;
using Fcs.Identity.Application.Abstractions.Messaging;
using Fcs.Identity.Application.Audit;
using Fcs.Identity.Domain.Abstractions;
using Fcs.Identity.Domain.ManagerProfiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fcs.Identity.Application.Seed;

public sealed class ManagerSeeder : IManagerSeeder
{
    private readonly IIdentityProvider _identityProvider;
    private readonly IManagerProfileRepository _managerProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ManagerSeedSettings _settings;
    private readonly ILogger<ManagerSeeder> _logger;

    public ManagerSeeder(
        IIdentityProvider identityProvider,
        IManagerProfileRepository managerProfileRepository,
        IUnitOfWork unitOfWork,
        IMessagePublisher messagePublisher,
        IOptions<ManagerSeedSettings> options,
        ILogger<ManagerSeeder> logger)
    {
        _identityProvider = identityProvider;
        _managerProfileRepository = managerProfileRepository;
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Manager seed is disabled.");
            return;
        }

        ValidateSettings();

        _logger.LogInformation("Manager seed started. Email: {Email}", _settings.Email);

        var identityResult = await _identityProvider.EnsureManagerAsync(
            new EnsureManagerIdentityUserRequest(_settings.FullName, _settings.Email, _settings.Password),
            cancellationToken);

        if (identityResult.IsFailure)
        {
            throw new InvalidOperationException($"Manager seed failed in identity provider. ErrorCode: {identityResult.Error.Code}");
        }

        var keycloakUserId = identityResult.Value.KeycloakUserId;
        var existingByEmail = await _managerProfileRepository.GetByEmailAsync(_settings.Email, cancellationToken);
        if (existingByEmail is not null && existingByEmail.KeycloakUserId != keycloakUserId)
        {
            throw new InvalidOperationException(
                $"Manager seed email '{_settings.Email}' is already linked to another Keycloak user id.");
        }

        var managerProfile = await _managerProfileRepository.GetByKeycloakUserIdAsync(keycloakUserId, cancellationToken);
        if (managerProfile is null)
        {
            var createResult = ManagerProfile.Create(keycloakUserId, _settings.FullName, _settings.Email);
            if (createResult.IsFailure)
            {
                throw new InvalidOperationException($"Manager seed profile creation failed. ErrorCode: {createResult.Error.Code}");
            }

            managerProfile = createResult.Value;
            await _managerProfileRepository.AddAsync(managerProfile, cancellationToken);
        }
        else
        {
            var updateResult = managerProfile.Update(_settings.FullName, _settings.Email);
            if (updateResult.IsFailure)
            {
                throw new InvalidOperationException($"Manager seed profile update failed. ErrorCode: {updateResult.Error.Code}");
            }

            _managerProfileRepository.Update(managerProfile);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _messagePublisher.PublishAuditLogFireAndForget(
            AuditLogRequestedEvent.Create(
                AuditActions.ManagerSeeded,
                nameof(ManagerProfile),
                entityId: managerProfile.Id.ToString(),
                metadata: new Dictionary<string, object?>
                {
                    ["email"] = _settings.Email,
                    ["keycloakUserId"] = keycloakUserId
                }));

        _logger.LogInformation(
            "Manager seed completed. ManagerProfileId: {ManagerProfileId}. KeycloakUserId: {KeycloakUserId}",
            managerProfile.Id,
            keycloakUserId);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.FullName))
        {
            throw new InvalidOperationException("Manager seed FullName must be configured.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Email))
        {
            throw new InvalidOperationException("Manager seed Email must be configured.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Password))
        {
            throw new InvalidOperationException("Manager seed Password must be configured.");
        }
    }
}
