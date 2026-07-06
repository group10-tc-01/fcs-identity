using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Fcs.Identity.WebApi.Swagger;

[ExcludeFromCodeCoverage]
public static class OpenApiExampleFactory
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public static JsonNode? Create(object value)
    {
        return JsonSerializer.SerializeToNode(value, JsonSerializerOptions);
    }
}
