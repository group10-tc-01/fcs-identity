using System.Diagnostics;
using System.Reflection;
using Fcs.Identity.Domain.Shared.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fcs.Identity.Application.Abstractions.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling application request {RequestName}", requestName);

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            LogCompletion(requestName, response, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            _logger.LogError(
                exception,
                "Application request {RequestName} failed with unhandled exception after {ElapsedMs} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private void LogCompletion(string requestName, TResponse response, long elapsedMs)
    {
        var result = TryReadResult(response);

        if (result is null)
        {
            _logger.LogInformation(
                "Application request {RequestName} completed after {ElapsedMs} ms",
                requestName,
                elapsedMs);

            return;
        }

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Application request {RequestName} completed successfully after {ElapsedMs} ms",
                requestName,
                elapsedMs);

            return;
        }

        _logger.LogWarning(
            "Application request {RequestName} completed with failure {ErrorCode} ({ErrorType}) after {ElapsedMs} ms: {ErrorMessage}",
            requestName,
            result.Error.Code,
            result.Error.Type,
            elapsedMs,
            result.Error.Message);
    }

    private static ResultDetails? TryReadResult(TResponse response)
    {
        if (response is null)
        {
            return null;
        }

        var responseType = response.GetType();
        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(Result<>))
        {
            return null;
        }

        var isSuccess = (bool)responseType.GetProperty(nameof(Result<object>.IsSuccess), BindingFlags.Instance | BindingFlags.Public)!.GetValue(response)!;
        if (isSuccess)
        {
            return new ResultDetails(true, Error.None);
        }

        var error = (Error)responseType.GetProperty(nameof(Result<object>.Error), BindingFlags.Instance | BindingFlags.Public)!.GetValue(response)!;

        return new ResultDetails(false, error);
    }

    private sealed record ResultDetails(bool IsSuccess, Error Error);
}
