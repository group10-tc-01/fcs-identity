using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.OpenApi.Any;

namespace Fcg.Identity.WebApi.Swagger;

[ExcludeFromCodeCoverage]
public static class OpenApiExampleFactory
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public static IOpenApiAny Create(object value)
    {
        var json = JsonSerializer.SerializeToElement(value, JsonSerializerOptions);

        return Create(json);
    }

    private static IOpenApiAny Create(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => CreateObject(value),
            JsonValueKind.Array => CreateArray(value),
            JsonValueKind.String => new OpenApiString(value.GetString()),
            JsonValueKind.Number when value.TryGetInt32(out var integer) => new OpenApiInteger(integer),
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => new OpenApiLong(longValue),
            JsonValueKind.Number => new OpenApiDouble(value.GetDouble()),
            JsonValueKind.True => new OpenApiBoolean(true),
            JsonValueKind.False => new OpenApiBoolean(false),
            JsonValueKind.Null => new OpenApiNull(),
            _ => new OpenApiString(value.ToString())
        };
    }

    private static OpenApiObject CreateObject(JsonElement value)
    {
        var result = new OpenApiObject();
        foreach (var property in value.EnumerateObject())
        {
            result[property.Name] = Create(property.Value);
        }

        return result;
    }

    private static OpenApiArray CreateArray(JsonElement value)
    {
        var result = new OpenApiArray();
        foreach (var item in value.EnumerateArray())
        {
            result.Add(Create(item));
        }

        return result;
    }
}
