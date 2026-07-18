using Fcs.Identity.Application.Abstractions.Identity;
using Fcs.Identity.Application.Abstractions.Messaging;
using Fcs.Identity.Application.Audit;
using Fcs.Identity.Application.IntegrationEvents.AuditLogs;
using Fcs.Identity.Application.IntegrationEvents.EmailNotifications;
using Fcs.Identity.Domain.Abstractions;
using Fcs.Identity.Domain.DonorProfiles;
using Fcs.Identity.Domain.Shared.Results;
using Fcs.Identity.Resources.Messages;
using Microsoft.Extensions.Logging;

namespace Fcs.Identity.Application.UseCases.Donors.RegisterDonor;

public sealed class RegisterDonorCommandHandler : ICommandHandler<RegisterDonorCommand, RegisterDonorResponse>
{
    private readonly IDonorProfileRepository _donorProfileRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IIdentityProvider _identityProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterDonorCommandHandler> _logger;

    public RegisterDonorCommandHandler(
        IDonorProfileRepository donorProfileRepository,
        IMessagePublisher messagePublisher,
        IIdentityProvider identityProvider,
        IUnitOfWork unitOfWork,
        ILogger<RegisterDonorCommandHandler> logger)
    {
        _donorProfileRepository = donorProfileRepository;
        _messagePublisher = messagePublisher;
        _identityProvider = identityProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<RegisterDonorResponse>> Handle(RegisterDonorCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.ToLowerInvariant();
        var normalizedCpf = new string(command.Cpf.Where(char.IsDigit).ToArray());

        _logger.LogInformation(
            "Register donor flow started. Email: {Email}. Cpf: {Cpf}",
            normalizedEmail,
            normalizedCpf);

        _logger.LogInformation("Checking donor uniqueness by email {Email}", normalizedEmail);
        if (await _donorProfileRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            _logger.LogWarning("Register donor flow stopped because email {Email} already exists", normalizedEmail);
            return Error.Conflict(IdentityErrorCodes.DonorProfileEmailAlreadyExists, IdentityMessages.DonorProfileEmailAlreadyExists);
        }

        _logger.LogInformation("Checking donor uniqueness by CPF {Cpf}", normalizedCpf);
        if (await _donorProfileRepository.ExistsByCpfAsync(normalizedCpf, cancellationToken))
        {
            _logger.LogWarning("Register donor flow stopped because CPF {Cpf} already exists", normalizedCpf);
            return Error.Conflict(IdentityErrorCodes.DonorProfileCpfAlreadyExists, IdentityMessages.CpfAlreadyExists);
        }

        _logger.LogInformation("Creating donor user in identity provider. Email: {Email}", normalizedEmail);
        var identityUserResult = await _identityProvider.CreateDonorAsync(
            new CreateDonorIdentityUserRequest(command.FullName, normalizedEmail, command.Password),
            cancellationToken);

        if (identityUserResult.IsFailure)
        {
            _logger.LogWarning(
                "Identity provider donor creation failed for email {Email}. ErrorCode: {ErrorCode}",
                normalizedEmail,
                identityUserResult.Error.Code);

            return identityUserResult.Error;
        }

        _logger.LogInformation(
            "Creating local donor profile. KeycloakUserId: {KeycloakUserId}. Email: {Email}",
            identityUserResult.Value.KeycloakUserId,
            normalizedEmail);

        var donorProfileResult = DonorProfile.Create(
            identityUserResult.Value.KeycloakUserId,
            command.FullName,
            normalizedEmail,
            normalizedCpf);

        if (donorProfileResult.IsFailure)
        {
            _logger.LogWarning(
                "Local donor profile creation failed for email {Email}. ErrorCode: {ErrorCode}",
                normalizedEmail,
                donorProfileResult.Error.Code);

            return donorProfileResult.Error;
        }

        var donorProfile = donorProfileResult.Value;

        _logger.LogInformation(
            "Persisting donor profile and audit log. DonorProfileId: {DonorProfileId}. KeycloakUserId: {KeycloakUserId}",
            donorProfile.Id,
            donorProfile.KeycloakUserId);

        await _donorProfileRepository.AddAsync(donorProfile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _messagePublisher.PublishAuditLogFireAndForget(
            AuditLogRequestedEvent.Create(
                AuditActions.DonorRegistered,
                nameof(DonorProfile),
                actorId: donorProfile.Id,
                actorType: "Doador",
                entityId: donorProfile.Id.ToString()));

        await PublishWelcomeNotificationAsync(donorProfile, cancellationToken);

        _logger.LogInformation(
            "Register donor flow completed. DonorProfileId: {DonorProfileId}. Email: {Email}",
            donorProfile.Id,
            normalizedEmail);

        return new RegisterDonorResponse(
            donorProfile.Id,
            donorProfile.FullName,
            donorProfile.Email.Value,
            MaskCpf(donorProfile.Cpf.Value));
    }

    private async Task PublishWelcomeNotificationAsync(DonorProfile donorProfile, CancellationToken cancellationToken)
    {
        try
        {
            await _messagePublisher.PublishAsync(
                KafkaTopicKeys.EmailNotification,
                new EmailNotificationRequestedEvent(Guid.NewGuid(), EmailNotificationRequestedEvent.DonorWelcome, donorProfile.Email.Value, null, null, DateTime.UtcNow),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Failed to publish donor welcome notification for donor {DonorProfileId}", donorProfile.Id);
        }
    }

    private static string MaskCpf(string cpf)
    {
        return $"***.***.***-{cpf[^2..]}";
    }
}
