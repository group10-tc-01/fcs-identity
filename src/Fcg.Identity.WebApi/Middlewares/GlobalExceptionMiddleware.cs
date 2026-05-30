using System.Net;
using System.Text.Json;
using Fcg.Identity.WebApi.Models;

namespace Fcg.Identity.WebApi.Middlewares;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteResponse(context, HttpStatusCode.InternalServerError, exception.Message);
        }
    }

    private static Task WriteResponse(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(ApiResponse<string>.FromFailure(message));
        return context.Response.WriteAsync(payload);
    }
}
