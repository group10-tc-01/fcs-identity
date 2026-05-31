using System.Diagnostics.CodeAnalysis;

namespace Fcg.Identity.WebApi.Swagger;

[ExcludeFromCodeCoverage]
public sealed record EndpointDocumentation(
    string Summary,
    string Description,
    object? RequestExample,
    IReadOnlyDictionary<string, ResponseDocumentation> Responses);

[ExcludeFromCodeCoverage]
public sealed record ResponseDocumentation(
    string Description,
    object? Example = null);
