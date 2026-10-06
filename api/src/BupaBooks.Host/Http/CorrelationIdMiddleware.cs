namespace BupaBooks.Host.Http;

/// <summary>
/// Accepts the caller's X-Correlation-ID (or creates one), echoes it on the response
/// and adds it to the logging scope, so one id ties a browser error to server logs.
/// Upstream calls carry the W3C traceparent automatically via OpenTelemetry.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = IsAcceptable(incoming) ? incoming : Guid.NewGuid().ToString("D");

        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    // Only accept short, simple values: the header is attacker-controlled and ends up in logs.
    private static bool IsAcceptable(string value) =>
        value.Length is > 0 and <= MaxLength && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
