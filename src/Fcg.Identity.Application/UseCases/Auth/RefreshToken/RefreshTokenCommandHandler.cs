using Fcg.Identity.Application.Abstractions.Identity;
using Fcg.Identity.Application.Abstractions.Messaging;
using Fcg.Identity.Application.Audit;
using Fcg.Identity.Application.UseCases.Auth.Login;
using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Domain.ManagerProfiles;
using Fcg.Identity.Domain.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Fcg.Identity.Application.UseCases.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IIdentityProvider _identityProvider;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IDonorProfileRepository _donorProfileRepository;
    private readonly IManagerProfileRepository _managerProfileRepository;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IIdentityProvider identityProvider,
        IMessagePublisher messagePublisher,
        IDonorProfileRepository donorProfileRepository,
        IManagerProfileRepository managerProfileRepository,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _identityProvider = identityProvider;
        _messagePublisher = messagePublisher;
        _donorProfileRepository = donorProfileRepository;
        _managerProfileRepository = managerProfileRepository;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refresh token flow started");
        _logger.LogInformation("Refreshing token with identity provider");

        var refreshResult = await _identityProvider.RefreshTokenAsync(
            new RefreshTokenIdentityUserRequest(command.RefreshToken),
            cancellationToken);

        if (refreshResult.IsFailure)
        {
            _logger.LogWarning(
                "Refresh token flow failed. ErrorCode: {ErrorCode}",
                refreshResult.Error.Code);

            return refreshResult.Error;
        }

        var actor = await AuditActorResolver.ResolveAsync(
            refreshResult.Value,
            _donorProfileRepository,
            _managerProfileRepository,
            cancellationToken);

        _logger.LogInformation(
            "Refresh token flow succeeded. Publishing audit log. ActorId: {ActorId}. ActorType: {ActorType}",
            actor.ActorId,
            actor.ActorType);

        _messagePublisher.PublishAuditLogFireAndForget(
            AuditLogRequestedEvent.Create(
                AuditActions.TokenRefreshed,
                "Authentication",
                actor.ActorId,
                actor.ActorType));

        _logger.LogInformation(
            "Refresh token flow completed. TokenType: {TokenType}. ExpiresIn: {ExpiresIn}",
            refreshResult.Value.TokenType,
            refreshResult.Value.ExpiresIn);

        return new LoginResponse(
            refreshResult.Value.AccessToken,
            refreshResult.Value.RefreshToken,
            refreshResult.Value.ExpiresIn,
            refreshResult.Value.TokenType);
    }
}
