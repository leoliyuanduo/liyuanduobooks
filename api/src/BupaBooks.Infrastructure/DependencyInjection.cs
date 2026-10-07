using BupaBooks.Application.Abstractions;
using BupaBooks.Infrastructure.BupaApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BupaBooks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<BupaApiOptions>()
            .Bind(configuration.GetSection(BupaApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var httpClient = services.AddHttpClient<BupaApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BupaApiOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        // Handlers run outermost-first in registration order:
        //   resilience (bulkhead -> total timeout -> retry -> circuit breaker -> attempt timeout)
        //     -> EmptyResponseGuardHandler -> network
        // so every attempt's response is checked, and an empty body is retried like a 5xx.
        // Retries only happen for transient failures (5xx, 408, 429, network errors); GET is idempotent.
        httpClient.AddStandardResilienceHandler(resilience =>
        {
            // Bulkhead: cap concurrent upstream calls so a slow upstream cannot tie up every
            // thread and connection in this service (cascading failure). HybridCache already
            // collapses identical requests, so 10 is generous.
            resilience.RateLimiter.DefaultRateLimiterOptions.PermitLimit = 10;
            resilience.RateLimiter.DefaultRateLimiterOptions.QueueLimit = 10;

            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
            resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);

            resilience.Retry.MaxRetryAttempts = 3;
            resilience.Retry.Delay = TimeSpan.FromMilliseconds(500); // exponential: ~0.5s, 1s, 2s (fits the 10s budget)
            resilience.Retry.UseJitter = true;

            // Circuit breaker: stop calling an upstream that is clearly down, fail fast, and let
            // the last-known-good fallback serve users. The default MinimumThroughput (100 calls
            // per sampling window) would never trip at this service's traffic, so it is lowered.
            resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            resilience.CircuitBreaker.MinimumThroughput = 5;
            resilience.CircuitBreaker.FailureRatio = 0.5;
            resilience.CircuitBreaker.BreakDuration = BupaApiClient.CircuitBreakDuration;
        });
        httpClient.AddHttpMessageHandler<EmptyResponseGuardHandler>();
        services.AddTransient<EmptyResponseGuardHandler>();

        services.AddHybridCache();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<LastKnownGoodOwners>();
        services.AddScoped<IBookOwnerSource, BupaBookOwnerSource>();

        return services;
    }
}
