using System.ComponentModel.DataAnnotations;

namespace BupaBooks.Infrastructure.BupaApi;

/// <summary>Settings for the upstream Bupa API. Validated at startup (fail fast on bad config).</summary>
public sealed class BupaApiOptions
{
    public const string SectionName = "BupaApi";

    [Required]
    public Uri BaseUrl { get; set; } = new("https://digitalcodingtest.bupa.com.au/");

    [Required]
    public string BookOwnersPath { get; set; } = "api/v1/bookowners";

    /// <summary>How long a successful upstream response is reused. The data changes rarely.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When upstream fails, serve the last successful response if it is at most this old
    /// (graceful degradation instead of an error). Zero disables the fallback.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "1.00:00:00")]
    public TimeSpan MaxStaleness { get; set; } = TimeSpan.FromHours(1);
}
