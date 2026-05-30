using Fcg.Identity.Domain.Shared.Results;
using Fcg.Identity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Identity.WebApi.Extensions;

public static class ErrorExtensions
{
    public static IActionResult ToActionResult(this Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(ApiResponse<string>.FromFailure(error.Message)),
            ErrorType.Conflict => new ConflictObjectResult(ApiResponse<string>.FromFailure(error.Message)),
            ErrorType.Validation => new BadRequestObjectResult(ApiResponse<string>.FromFailure(error.Message)),
            _ => new ObjectResult(ApiResponse<string>.FromFailure(error.Message)) { StatusCode = StatusCodes.Status500InternalServerError }
        };
}
