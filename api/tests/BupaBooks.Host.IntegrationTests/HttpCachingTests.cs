using System.Text.Json;
using BupaBooks.Host.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace BupaBooks.Host.IntegrationTests;

/// <summary>Unit tests for the ETag helper (regression: a weak ETag string was once parsed as a tag and threw).</summary>
public class HttpCachingTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private static bool Matches(string? ifNoneMatch, EntityTagHeaderValue etag)
    {
        var context = new DefaultHttpContext();
        if (ifNoneMatch is not null)
        {
            context.Request.Headers.IfNoneMatch = ifNoneMatch;
        }

        return HttpCaching.MatchesIfNoneMatch(context.Request, etag);
    }

    [Fact]
    public void The_etag_is_a_weak_validator_derived_from_the_content()
    {
        var first = HttpCaching.ComputeETag(new { value = 1 }, Options);

        first.IsWeak.ShouldBeTrue();
        first.ToString().ShouldStartWith("W/\"");
        HttpCaching.ComputeETag(new { value = 1 }, Options).ShouldBe(first);
        HttpCaching.ComputeETag(new { value = 2 }, Options).ShouldNotBe(first);
    }

    [Fact]
    public void If_none_match_uses_weak_comparison()
    {
        var etag = HttpCaching.ComputeETag(new { value = 1 }, Options);

        Matches(etag.ToString(), etag).ShouldBeTrue(); // W/"…" as the server sent it
        Matches(etag.Tag.ToString(), etag).ShouldBeTrue(); // same tag without W/
        Matches($"\"something-else\", {etag}", etag).ShouldBeTrue(); // one of several
        Matches("*", etag).ShouldBeTrue();
        Matches("\"something-else\"", etag).ShouldBeFalse();
        Matches(null, etag).ShouldBeFalse();
    }
}
