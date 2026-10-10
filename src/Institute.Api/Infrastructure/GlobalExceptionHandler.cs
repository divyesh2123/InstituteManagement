using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Institute.Api.Infrastructure;

/// <summary>
/// Last line of defence: converts unhandled exceptions into RFC 7807 problem details without leaking internals.
/// Expected business failures never reach this handler; they flow as <c>Result</c> failures.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    // SQL Server error numbers.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    private const int ForeignKeyOrCheckViolation = 547;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                "The record was modified by someone else. Reload it and try again."),
            DbUpdateException { InnerException: SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation } } => (
                StatusCodes.Status409Conflict,
                "Duplicate value",
                "A record with the same unique value already exists."),
            DbUpdateException { InnerException: SqlException { Number: ForeignKeyOrCheckViolation } } => (
                StatusCodes.Status409Conflict,
                "Constraint violation",
                "The operation violates a data integrity rule."),
            BadHttpRequestException bad => (bad.StatusCode, "Bad request", bad.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                "An unexpected error occurred. Use the traceId when contacting support."),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, exception, httpContext.TraceIdentifier);
        }
        else
        {
            LogHandled(logger, exception.GetType().Name, status, httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Type = $"https://httpstatuses.io/{status}",
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception (traceId {TraceId})")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string traceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{ExceptionType} mapped to HTTP {StatusCode} (traceId {TraceId})")]
    private static partial void LogHandled(ILogger logger, string exceptionType, int statusCode, string traceId);
}
