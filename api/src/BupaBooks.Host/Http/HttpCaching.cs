using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Net.Http.Headers;

namespace BupaBooks.Host.Http;

/// <summary>
/// Conditional GET support. The ETag is a hash of the exact response body, so a
/// client that already has the current representation gets a 304 with no body.
/// </summary>
internal static class HttpCaching
{
    public const string CacheControlValue = "private, max-age=60";

    public static string ComputeETag<T>(T body, JsonSerializerOptions options)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(body, options);
        return $"\"{Convert.ToHexStringLower(SHA256.HashData(bytes))[..32]}\"";
    }

    public static bool MatchesIfNoneMatch(HttpRequest request, string etag)
    {
        var candidates = request.GetTypedHeaders().IfNoneMatch;
        if (candidates is null || candidates.Count == 0)
        {
            return false;
        }

        var current = new EntityTagHeaderValue(etag);
        return candidates.Any(candidate =>
            candidate.Equals(EntityTagHeaderValue.Any) || candidate.Compare(current, useStrongComparison: false));
    }
}
