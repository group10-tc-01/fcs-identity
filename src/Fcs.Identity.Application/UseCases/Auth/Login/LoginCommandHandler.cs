using Fcs.Identity.Application.Abstractions.Identity;
using Fcs.Identity.Application.Abstractions.Messaging;
using Fcs.Identity.Application.Audit;
using Fcs.Identity.Application.IntegrationEvents.AuditLogs;
using Fcs.Identity.Domain.DonorProfiles;
using Fcs.Identity.Domain.ManagerProfiles;
using Fcs.Identity.Domain.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Fcs.Identity.Application.UseCases.Auth.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IIdentityProvider _identityProvider;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IDonorProfileRepository _donorProfileRepository;
    private readonly IManagerProfileRepository _managerProfileRepository;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IIdentityProvider identityProvider,
        IMessagePublisher messagePublisher,
        IDonorProfileRepository donorProfileRepository,
        IManagerProfileRepository managerProfileRepository,
        ILogger<LoginCommandHandler> logger)
    {
        _identityProvider = identityProvider;
        _messagePublisher = messagePublisher;
        _donorProfileRepository = donorProfileRepository;
        _managerProfileRepository = managerProfileRepository;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login flow started. Email: {Email}", command.Email);
        _logger.LogInformation("Authenticating user with identity provider. Email: {Email}", command.Email);

        var loginResult = await _identityProvider.LoginAsync(new LoginIdentityUserRequest(command.Email, command.Password), cancellationToken);

        if (loginResult.IsFailure)
        {
            _logger.LogWarning(
                "Login flow failed for email {Email}. ErrorCode: {ErrorCode}",
                command.Email,
                loginResult.Error.Code);

            AddPublicAuditLog(AuditActions.LoginFailed, command.Email);
            return loginResult.Error;
        }

        _logger.LogInformation("Login flow succeeded for email {Email}. Publishing audit log", command.Email);
        await AddAuthenticatedAuditLogAsync(AuditActions.LoginSucceeded, command.Email, loginResult.Value, cancellationToken);

        _logger.LogInformation(
            "Login flow completed for email {Email}. TokenType: {TokenType}. ExpiresIn: {ExpiresIn}",
            command.Email,
            loginResult.Value.TokenType,
            loginResult.Value.ExpiresIn);

        return new LoginResponse(
            loginResult.Value.AccessToken,
            loginResult.Value.RefreshToken,
            loginResult.Value.ExpiresIn,
            loginResult.Value.TokenType);
    }

    private void AddPublicAuditLog(string action, string email)
    {
        _logger.LogInformation("Publishing login audit log. Action: {AuditAction}. Email: {Email}", action, email);

        _messagePublisher.PublishAuditLogFireAndForget(
            AuditLogRequestedEvent.Create(
                action,
                "Authentication",
                actorType: "Public",
                metadata: new Dictionary<string, object?> { ["email"] = email }));
    }

    private async Task AddAuthenticatedAuditLogAsync(
        string action,
        string email,
        LoginIdentityUserResponse token,
        CancellationToken cancellationToken)
    {
        var actor = await AuditActorResolver.ResolveAsync(
            token,
            _donorProfileRepository,
            _managerProfileRepository,
            cancellationToken);

        _logger.LogInformation(
            "Publishing login audit log. Action: {AuditAction}. ActorId: {ActorId}. ActorType: {ActorType}",
            action,
            actor.ActorId,
            actor.ActorType);

        _messagePublisher.PublishAuditLogFireAndForget(
            AuditLogRequestedEvent.Create(
                action,
                "Authentication",
                actor.ActorId,
                actor.ActorType,
                metadata: new Dictionary<string, object?> { ["email"] = email }));
    }
}
