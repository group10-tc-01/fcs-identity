using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fcs.Identity.WebApi.Swagger;

[ExcludeFromCodeCoverage]
public sealed class SwaggerAuthorizationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true ||
            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

        if (!hasAuthorize)
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(SwaggerConstants.BearerSecurityScheme, context.Document, null),
                    []
                }
            }
        ];
    }
}
