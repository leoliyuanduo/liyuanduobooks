using BupaBooks.Application.Abstractions;
using BupaBooks.Infrastructure.BupaApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services
            .AddHttpClient<BupaApiClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<BupaApiOptions>>().Value;
                client.BaseAddress = options.BaseUrl;
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            // Standard pipeline: rate limiter -> total timeout -> retry -> circuit breaker -> attempt timeout.
            // Retries only on transient failures (5xx, 408, 429, network errors); GET is idempotent so this is safe.
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
                resilience.Retry.MaxRetryAttempts = 2;
                resilience.Retry.UseJitter = true;
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                resilience.CircuitBreaker.BreakDuration = BupaApiClient.CircuitBreakDuration;
            });

        services.AddHybridCache();
        services.AddScoped<IBookOwnerSource, BupaBookOwnerSource>();

        return services;
    }
}
