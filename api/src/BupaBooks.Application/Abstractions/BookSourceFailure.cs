namespace BupaBooks.Application.Abstractions;

/// <summary>Why the book source failed. The Host maps each value to an HTTP status.</summary>
public enum BookSourceFailure
{
    /// <summary>The source did not respond in time (maps to 504).</summary>
    Timeout,

    /// <summary>The source is temporarily refused, e.g. circuit breaker open (maps to 503).</summary>
    Unavailable,

    /// <summary>The source returned an error status (maps to 502).</summary>
    UpstreamError,

    /// <summary>The source returned data that does not match the expected contract (maps to 502).</summary>
    InvalidResponse,
}
