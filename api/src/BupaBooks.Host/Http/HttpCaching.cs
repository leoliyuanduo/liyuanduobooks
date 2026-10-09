using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Net.Http.Headers;

namespace BupaBooks.Host.Http;

/// <summary>
/// Conditional GET support. The ETag is a hash of the response content, so a client
/// that already has the current representation gets a 304 with no body. It is a WEAK
/// validator (W/"…"): the same content may be sent gzip, Brotli or uncompressed, and a
/// strong ETag would promise byte-for-byte identity across those encodings.
/// <para>
/// ETags are handled as <see cref="EntityTagHeaderValue"/>, never as raw strings: the
/// "W/" prefix is part of the header syntax, not of the tag, and the typed API keeps
/// formatting and parsing consistent.
/// </para>
/// </summary>
internal static class HttpCaching
{
    public const string CacheControlValue = "private, max-age=60";

    public static EntityTagHeaderValue ComputeETag<T>(T body, JsonSerializerOptions options)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(body, options);
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes))[..32];
        return new EntityTagHeaderValue($"\"{hash}\"", isWeak: true);
    }

    /// <summary>True if the request's If-None-Match matches (weak comparison, per RFC 9110 §13.1.2).</summary>
    public static bool MatchesIfNoneMatch(HttpRequest request, EntityTagHeaderValue etag)
    {
        var candidates = request.GetTypedHeaders().IfNoneMatch;
        if (candidates is null || candidates.Count == 0)
        {
            return false;
        }

        return candidates.Any(candidate =>
            candidate.Equals(EntityTagHeaderValue.Any) || candidate.Compare(etag, useStrongComparison: false));
    }
}
