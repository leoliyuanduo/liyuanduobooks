using BupaBooks.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace BupaBooks.Infrastructure;

/// <summary>
/// Structured log messages, source-generated (no boxing, no string formatting
/// when the level is disabled). Event ids are stable so alerts can key on them.
/// Messages never include personal data.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "Skipped upstream book owner at index {OwnerIndex}: {Reason}")]
    public static partial void OwnerRecordSkipped(ILogger logger, int ownerIndex, string reason);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Skipped upstream book {BookIndex} of owner at index {OwnerIndex}: {Reason}")]
    public static partial void BookRecordSkipped(ILogger logger, int ownerIndex, int bookIndex, string reason);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information,
        Message = "Fetched {OwnerCount} book owners from upstream in {ElapsedMs} ms")]
    public static partial void BookOwnersFetched(ILogger logger, int ownerCount, long elapsedMs);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning,
        Message = "Upstream book source failed: {Failure}")]
    public static partial void UpstreamFailed(ILogger logger, Exception exception, BookSourceFailure failure);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Warning,
        Message = "Upstream failed ({Failure}); serving last known good data, {AgeSeconds} s old")]
    public static partial void ServingStaleData(ILogger logger, BookSourceFailure failure, long ageSeconds);
}
