using Fcg.Identity.Domain.Shared.Results;
using MediatR;

namespace Fcg.Identity.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
