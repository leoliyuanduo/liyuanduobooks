using System.Globalization;
using System.Net;
using BupaBooks.Application.Abstractions;
using BupaBooks.Infrastructure.BupaApi;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BupaBooks.Infrastructure.Tests;

/// <summary>
/// The real AddInfrastructure() wiring (cache + resilience + client + mapper),
/// with only the network replaced.
/// </summary>
public class BupaBookOwnerSourceTests
{
    /// <summary>A clock the test controls, to make "data is too old" deterministic.</summary>
    private sealed class ManualTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } =
            DateTimeOffset.Parse("2026-10-07T00:00:00Z", CultureInfo.InvariantCulture);

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private static ServiceProvider BuildServices(StubHttpHandler upstream, TimeProvider? clock = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BupaApi:BaseUrl"] = "https://upstream.test/" })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.AddHttpClient<BupaApiClient>().ConfigurePrimaryHttpMessageHandler(() => upstream);
        if (clock is not null)
        {
            services.AddSingleton(clock);
        }

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Maps_the_live_payload_to_domain_owners()
    {
        await using var services = BuildServices(StubHttpHandler.Json(TestData.LivePayload()));
        var source = services.GetRequiredService<IBookOwnerSource>();

        var owners = await source.GetOwnersAsync(CancellationToken.None);

        owners.Count.ShouldBe(5);
        owners.SelectMany(owner => owner.Books).Select(book => book.Id).Distinct().Count().ShouldBe(10);
    }

    [Fact]
    public async Task Repeated_calls_are_served_from_cache()
    {
        var upstream = StubHttpHandler.Json(TestData.LivePayload());
        await using var services = BuildServices(upstream);
        var source = services.GetRequiredService<IBookOwnerSource>();

        await source.GetOwnersAsync(CancellationToken.None);
        await source.GetOwnersAsync(CancellationToken.None);

        upstream.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Concurrent_requests_on_a_cold_cache_make_one_upstream_call()
    {
        var upstream = new StubHttpHandler(async (_, token) =>
        {
            await Task.Delay(200, token);
            return StubHttpHandler.JsonResponse(TestData.LivePayload());
        });
        await using var services = BuildServices(upstream);

        // Each request gets its own scope, as in ASP.NET Core.
        var calls = Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var scope = services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IBookOwnerSource>().GetOwnersAsync(CancellationToken.None);
        });
        await Task.WhenAll(calls);

        upstream.Calls.ShouldBe(1); // stampede protection: duplicate-request prevention
    }

    [Fact]
    public async Task Transient_upstream_errors_are_retried()
    {
        var attempt = 0;
        var upstream = new StubHttpHandler((_, _) => Task.FromResult(
            Interlocked.Increment(ref attempt) == 1
                ? StubHttpHandler.JsonResponse("{}", HttpStatusCode.ServiceUnavailable)
                : StubHttpHandler.JsonResponse(TestData.LivePayload())));
        await using var services = BuildServices(upstream);

        var owners = await services.GetRequiredService<IBookOwnerSource>().GetOwnersAsync(CancellationToken.None);

        owners.Count.ShouldBe(5);
        upstream.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task Empty_upstream_responses_are_retried()
    {
        // Observed behaviour of the real API: intermittent 200 OK with an empty body.
        var attempt = 0;
        var upstream = new StubHttpHandler((_, _) => Task.FromResult(
            Interlocked.Increment(ref attempt) <= 2
                ? StubHttpHandler.JsonResponse(string.Empty)
                : StubHttpHandler.JsonResponse(TestData.LivePayload())));
        await using var services = BuildServices(upstream);

        var owners = await services.GetRequiredService<IBookOwnerSource>().GetOwnersAsync(CancellationToken.None);

        owners.Count.ShouldBe(5);
        upstream.Calls.ShouldBe(3);
    }

    [Fact]
    public async Task Rate_limited_responses_are_retried()
    {
        var attempt = 0;
        var upstream = new StubHttpHandler((_, _) => Task.FromResult(
            Interlocked.Increment(ref attempt) == 1
                ? StubHttpHandler.JsonResponse("\"Too many requests\"", HttpStatusCode.TooManyRequests)
                : StubHttpHandler.JsonResponse(TestData.LivePayload())));
        await using var services = BuildServices(upstream);

        var owners = await services.GetRequiredService<IBookOwnerSource>().GetOwnersAsync(CancellationToken.None);

        owners.Count.ShouldBe(5);
        upstream.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task Failures_are_not_cached()
    {
        var attempt = 0;
        var upstream = new StubHttpHandler((_, _) => Task.FromResult(
            Interlocked.Increment(ref attempt) == 1
                ? StubHttpHandler.JsonResponse("not json")
                : StubHttpHandler.JsonResponse(TestData.LivePayload())));
        await using var services = BuildServices(upstream);
        var source = services.GetRequiredService<IBookOwnerSource>();

        await Should.ThrowAsync<BookSourceException>(() => source.GetOwnersAsync(CancellationToken.None));
        (await source.GetOwnersAsync(CancellationToken.None)).Count.ShouldBe(5);
    }

    /// <summary>Upstream answers once with the live payload, then with an unrecoverable error.</summary>
    private static StubHttpHandler HealthyThenBroken()
    {
        var attempt = 0;
        return new StubHttpHandler((_, _) => Task.FromResult(
            Interlocked.Increment(ref attempt) == 1
                ? StubHttpHandler.JsonResponse(TestData.LivePayload())
                : StubHttpHandler.JsonResponse("not json")));
    }

    [Fact]
    public async Task Serves_last_known_good_data_when_upstream_fails()
    {
        var upstream = HealthyThenBroken();
        await using var services = BuildServices(upstream);
        var source = services.GetRequiredService<IBookOwnerSource>();

        await source.GetOwnersAsync(CancellationToken.None);
        await services.GetRequiredService<HybridCache>().RemoveAsync(BupaBookOwnerSource.CacheKey);

        var owners = await source.GetOwnersAsync(CancellationToken.None);

        owners.Count.ShouldBe(5); // stale, but useful
        upstream.Calls.ShouldBe(2); // upstream really was asked again
    }

    [Fact]
    public async Task Without_earlier_data_the_failure_is_reported()
    {
        await using var services = BuildServices(StubHttpHandler.Json("not json"));

        var ex = await Should.ThrowAsync<BookSourceException>(
            () => services.GetRequiredService<IBookOwnerSource>().GetOwnersAsync(CancellationToken.None));

        ex.Failure.ShouldBe(BookSourceFailure.InvalidResponse);
    }

    [Fact]
    public async Task Data_older_than_the_staleness_limit_is_not_served()
    {
        var clock = new ManualTimeProvider();
        await using var services = BuildServices(HealthyThenBroken(), clock);
        var source = services.GetRequiredService<IBookOwnerSource>();

        await source.GetOwnersAsync(CancellationToken.None);
        await services.GetRequiredService<HybridCache>().RemoveAsync(BupaBookOwnerSource.CacheKey);
        clock.UtcNow += TimeSpan.FromHours(2); // default MaxStaleness is 1 hour

        await Should.ThrowAsync<BookSourceException>(() => source.GetOwnersAsync(CancellationToken.None));
    }
}
