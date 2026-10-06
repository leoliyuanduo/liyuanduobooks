using BupaBooks.Application.Abstractions;
using BupaBooks.Application.Domain;
using BupaBooks.Infrastructure.BupaApi;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace BupaBooks.Infrastructure;

/// <summary>
/// Adapter implementing the Application's port: cache, then HTTP, then the
/// anti-corruption mapper.
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
    IOptions<BupaApiOptions> options) : IBookOwnerSource
{
    private const string CacheKey = "bupa:bookowners:v1";

    public async Task<IReadOnlyList<Owner>> GetOwnersAsync(CancellationToken cancellationToken)
    {
        var duration = options.Value.CacheDuration;
        var entryOptions = new HybridCacheEntryOptions { Expiration = duration, LocalCacheExpiration = duration };

        var dtos = await cache.GetOrCreateAsync(
            CacheKey,
            async token => await client.GetBookOwnersAsync(token).ConfigureAwait(false),
            entryOptions,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return BupaOwnerMapper.ToDomain(dtos);
    }
}
