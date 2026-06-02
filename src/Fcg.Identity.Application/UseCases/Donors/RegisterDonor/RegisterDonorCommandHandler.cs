using Fcg.Identity.Application.Abstractions.Identity;
using Fcg.Identity.Application.Abstractions.Messaging;
using Fcg.Identity.Application.Audit;
using Fcg.Identity.Domain.Abstractions;
using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Domain.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Fcg.Identity.Application.UseCases.Donors.RegisterDonor;

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
            return Error.Conflict("DonorProfile.EmailAlreadyExists", "A donor profile with this email already exists.");
        }

        _logger.LogInformation("Checking donor uniqueness by CPF {Cpf}", normalizedCpf);
        if (await _donorProfileRepository.ExistsByCpfAsync(normalizedCpf, cancellationToken))
        {
            _logger.LogWarning("Register donor flow stopped because CPF {Cpf} already exists", normalizedCpf);
            return Error.Conflict("DonorProfile.CpfAlreadyExists", "A donor profile with this CPF already exists.");
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

    private static string MaskCpf(string cpf)
    {
        return $"***.***.***-{cpf[^2..]}";
    }
}
