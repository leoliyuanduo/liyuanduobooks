using System.Globalization;
using BupaBooks.Application.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BupaBooks.Host.ErrorHandling;

/// <summary>
/// Maps upstream failures to gateway status codes as RFC 9457 problem details.
/// Clients get a stable, documented error shape and a traceId; internal details
/// (exception messages, upstream URLs) stay in the logs.
/// </summary>
internal sealed partial class BookSourceExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<BookSourceExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BookSourceException sourceException)
        {
            return false;
        }

        var (status, title) = sourceException.Failure switch
        {
            BookSourceFailure.Timeout => (StatusCodes.Status504GatewayTimeout, "The book service timed out."),
            BookSourceFailure.Unavailable => (StatusCodes.Status503ServiceUnavailable, "The book service is temporarily unavailable."),
            BookSourceFailure.UpstreamError => (StatusCodes.Status502BadGateway, "The book service returned an error."),
            BookSourceFailure.InvalidResponse => (StatusCodes.Status502BadGateway, "The book service returned an unexpected response."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        LogUpstreamFailure(logger, sourceException.Failure, status);

        if (sourceException.RetryAfter is { } retryAfter)
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title },
        });
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning,
        Message = "Book source failure {Failure} returned to client as HTTP {StatusCode}")]
    private static partial void LogUpstreamFailure(ILogger logger, BookSourceFailure failure, int statusCode);
}
