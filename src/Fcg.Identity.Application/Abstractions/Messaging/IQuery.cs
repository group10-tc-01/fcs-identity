using Fcg.Identity.Domain.Shared.Results;
using MediatR;

namespace Fcg.Identity.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
