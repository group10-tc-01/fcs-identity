using Fcg.Identity.Domain.Shared.Results;
using MediatR;

namespace Fcg.Identity.Application.Abstractions.Messaging;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
