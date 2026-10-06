namespace BupaBooks.Application.Abstractions;

/// <summary>
/// The only exception the book source is allowed to surface. Infrastructure
/// translates transport errors (HttpRequestException, Polly timeouts, JSON errors)
/// into this, so outer layers never depend on transport details.
/// </summary>
public sealed class BookSourceException : Exception
{
    public BookSourceFailure Failure { get; }

    /// <summary>When set, a hint for how long the caller should wait before retrying.</summary>
    public TimeSpan? RetryAfter { get; }

    public BookSourceException(
        BookSourceFailure failure,
        string message,
        Exception? innerException = null,
        TimeSpan? retryAfter = null)
        : base(message, innerException)
    {
        Failure = failure;
        RetryAfter = retryAfter;
    }
}
