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
}
