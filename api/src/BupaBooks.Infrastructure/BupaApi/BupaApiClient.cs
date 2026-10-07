using System.Diagnostics;
using System.Text.Json;
using BupaBooks.Application.Abstractions;
using BupaBooks.Infrastructure.BupaApi.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;

namespace BupaBooks.Infrastructure.BupaApi;

/// <summary>
/// Typed HttpClient for the Bupa API. Retries, timeouts and the circuit breaker
/// are attached in <see cref="DependencyInjection"/>; this class translates
/// whatever they throw into <see cref="BookSourceException"/>.
/// </summary>
public sealed class BupaApiClient(HttpClient httpClient, IOptions<BupaApiOptions> options, ILogger<BupaApiClient> logger)
{
    /// <summary>Matches the circuit breaker's break duration, so clients know when to come back.</summary>
    internal static readonly TimeSpan CircuitBreakDuration = TimeSpan.FromSeconds(15);

    public async Task<BupaBookOwnerDto[]> GetBookOwnersAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await httpClient
                .GetAsync(options.Value.BookOwnersPath, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BookSourceException(
                    BookSourceFailure.UpstreamError,
                    $"Upstream returned HTTP {(int)response.StatusCode}.");
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                using var document = await JsonDocument
                    .ParseAsync(stream, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var owners = BupaPayloadReader.Read(document.RootElement, logger);
                Log.BookOwnersFetched(logger, owners.Length, stopwatch.ElapsedMilliseconds);
                return owners;
            }
        }
        catch (BookSourceException ex)
        {
            Log.UpstreamFailed(logger, ex, ex.Failure);
            throw;
        }
        catch (Exception ex) when (Translate(ex, cancellationToken) is { } translated)
        {
            Log.UpstreamFailed(logger, ex, translated.Failure);
            throw translated;
        }
    }

    /// <summary>Maps transport-level failures to the Application's failure vocabulary.</summary>
    private static BookSourceException? Translate(Exception exception, CancellationToken cancellationToken) =>
        exception switch
        {
            // The caller went away: not an upstream failure, let it propagate unchanged.
            OperationCanceledException when cancellationToken.IsCancellationRequested => null,

            TimeoutRejectedException => new(BookSourceFailure.Timeout, "Upstream did not respond in time.", exception),
            OperationCanceledException => new(BookSourceFailure.Timeout, "Upstream did not respond in time.", exception),
            BrokenCircuitException => new(
                BookSourceFailure.Unavailable,
                "Upstream is temporarily unavailable.",
                exception,
                CircuitBreakDuration),
            RateLimiterRejectedException => new(
                BookSourceFailure.Unavailable,
                "Too many concurrent upstream requests.",
                exception,
                TimeSpan.FromSeconds(1)),
            HttpRequestException => new(BookSourceFailure.UpstreamError, "Upstream request failed.", exception),
            JsonException => new(BookSourceFailure.InvalidResponse, "Upstream returned invalid JSON.", exception),
            _ => null,
        };
}
