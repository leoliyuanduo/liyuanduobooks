using BupaBooks.Application.Abstractions;
using BupaBooks.Application.Domain;
using BupaBooks.Infrastructure.BupaApi;
using BupaBooks.Infrastructure.BupaApi.Dtos;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BupaBooks.Infrastructure;

/// <summary>
/// Adapter implementing the Application's port: cache, then HTTP (with resilience),
/// then the anti-corruption mapper, with a last-known-good fallback.
/// <para>
/// HybridCache gives stampede protection: if many requests arrive while the cache
/// is empty, only one upstream call is made and the others await it. Failures are
/// not cached. We cache the validated upstream DTOs (plain, serialisable records),
/// not domain objects, and map on read; mapping a few records is cheap.
/// </para>
/// </summary>
internal sealed class BupaBookOwnerSource(
    BupaApiClient client,
    HybridCache cache,
    LastKnownGoodOwners lastKnownGood,
    IOptions<BupaApiOptions> options,
    ILogger<BupaBookOwnerSource> logger) : IBookOwnerSource
{
    internal const string CacheKey = "bupa:bookowners:v1";

    public async Task<IReadOnlyList<Owner>> GetOwnersAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = settings.CacheDuration,
            LocalCacheExpiration = settings.CacheDuration,
        };

        BupaBookOwnerDto[] dtos;
        try
        {
            dtos = await cache.GetOrCreateAsync(
                CacheKey,
                async token =>
                {
                    var fresh = await client.GetBookOwnersAsync(token).ConfigureAwait(false);
                    lastKnownGood.Store(fresh);
                    return fresh;
                },
                entryOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (BookSourceException ex)
        {
            // Graceful degradation: slightly old data beats an error page. Nothing is cached,
            // so the next request tries upstream again (or fails fast while the circuit is open).
            if (!lastKnownGood.TryGet(settings.MaxStaleness, out var stale, out var age))
            {
                throw;
            }

            Log.ServingStaleData(logger, ex.Failure, (long)age.TotalSeconds);
            dtos = stale;
        }

        return BupaOwnerMapper.ToDomain(dtos);
    }
}
