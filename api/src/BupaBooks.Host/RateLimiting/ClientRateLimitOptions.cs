using System.ComponentModel.DataAnnotations;

namespace BupaBooks.Host.RateLimiting;

/// <summary>
/// Per-client token bucket. Defaults allow a burst of 20 requests, refilling 10 every
/// 10 seconds (about 1 request/second sustained). One screen load is 2 requests, so a
/// real user never notices; a script hammering the API is slowed to a safe rate.
/// </summary>
public sealed class ClientRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, 10_000)]
    public int PermitLimit { get; set; } = 20;

    [Range(1, 10_000)]
    public int TokensPerPeriod { get; set; } = 10;

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan ReplenishmentPeriod { get; set; } = TimeSpan.FromSeconds(10);
}
