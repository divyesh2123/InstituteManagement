using System.Diagnostics;
using Institute.Domain.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Institute.Application.Behaviors;

internal sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var startTimestamp = Stopwatch.GetTimestamp();

        LogHandling(logger, requestName);

        var response = await next();
        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        if (response.IsFailure)
        {
            LogFailed(logger, requestName, response.Error.Code, response.Error.Description, elapsedMs);
        }
        else
        {
            LogHandled(logger, requestName, elapsedMs);
        }

        return response;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
    private static partial void LogHandling(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMs:0.0} ms")]
    private static partial void LogHandled(ILogger logger, string requestName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RequestName} failed with {ErrorCode}: {ErrorDescription} ({ElapsedMs:0.0} ms)")]
    private static partial void LogFailed(ILogger logger, string requestName, string errorCode, string errorDescription, double elapsedMs);
}
