using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Identity.WebApi.Controllers.v1;

[ExcludeFromCodeCoverage]
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController(IMediator mediator) : ControllerBase
{
    protected IMediator _mediator { get; } = mediator;
}
