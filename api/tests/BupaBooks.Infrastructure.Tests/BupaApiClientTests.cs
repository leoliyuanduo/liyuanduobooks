using System.Net;
using BupaBooks.Application.Abstractions;
using BupaBooks.Infrastructure.BupaApi;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;

namespace BupaBooks.Infrastructure.Tests;

/// <summary>
/// The client in isolation (no resilience pipeline): payload validation and
/// translation of transport failures into the Application's vocabulary.
/// </summary>
public class BupaApiClientTests
{
    private static BupaApiClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://upstream.test/") },
            Options.Create(new BupaApiOptions()),
            NullLogger<BupaApiClient>.Instance);

    [Fact]
    public async Task Reads_the_live_payload()
    {
        var owners = await CreateClient(StubHttpHandler.Json(TestData.LivePayload())).GetBookOwnersAsync(CancellationToken.None);

        owners.Select(owner => owner.Name).ShouldBe(["Jane", "Charlotte", "Max", "William", "Charles"]);
        owners.Sum(owner => owner.Books!.Length).ShouldBe(10);
    }

    [Fact]
    public async Task Calls_the_configured_path()
    {
        Uri? requested = null;
        var handler = new StubHttpHandler((request, _) =>
        {
            requested = request.RequestUri;
            return Task.FromResult(StubHttpHandler.JsonResponse("[]"));
        });

        await CreateClient(handler).GetBookOwnersAsync(CancellationToken.None);

        requested.ShouldBe(new Uri("https://upstream.test/api/v1/bookowners"));
    }

    [Fact]
    public async Task Skips_malformed_records_and_keeps_valid_ones()
    {
        const string payload = """
            [
              { "name": "Valid", "age": 30, "books": [ { "name": "Kept", "type": "Hardcover" }, { "type": "Paperback" } ] },
              { "name": "Age as text", "age": "30", "books": [] },
              { "name": "Negative age", "age": -1, "books": [] },
              { "age": 40, "books": [] },
              { "name": "No books", "age": 12, "books": null },
              null
            ]
            """;

        var owners = await CreateClient(StubHttpHandler.Json(payload)).GetBookOwnersAsync(CancellationToken.None);

        owners.Select(owner => owner.Name).ShouldBe(["Valid", "No books"]);
        owners[0].Books!.Select(book => book.Name).ShouldBe(["Kept"]);
        owners[1].Books!.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{ "owners": [] }""")]
    [InlineData("<html>maintenance</html>")]
    public async Task A_payload_that_breaks_the_contract_is_an_invalid_response(string body)
    {
        var ex = await Should.ThrowAsync<BookSourceException>(
            () => CreateClient(StubHttpHandler.Json(body)).GetBookOwnersAsync(CancellationToken.None));

        ex.Failure.ShouldBe(BookSourceFailure.InvalidResponse);
    }

    [Fact]
    public async Task An_error_status_is_an_upstream_error()
    {
        var ex = await Should.ThrowAsync<BookSourceException>(
            () => CreateClient(StubHttpHandler.Json("{}", HttpStatusCode.InternalServerError)).GetBookOwnersAsync(CancellationToken.None));

        ex.Failure.ShouldBe(BookSourceFailure.UpstreamError);
    }

    public static TheoryData<Exception, BookSourceFailure> TransportFailures => new()
    {
        { new HttpRequestException("connection refused"), BookSourceFailure.UpstreamError },
        { new TimeoutRejectedException("attempt timed out"), BookSourceFailure.Timeout },
        { new TaskCanceledException("HttpClient.Timeout elapsed"), BookSourceFailure.Timeout },
        { new BrokenCircuitException("circuit open"), BookSourceFailure.Unavailable },
        { new RateLimiterRejectedException("bulkhead full"), BookSourceFailure.Unavailable },
    };

    [Theory]
    [MemberData(nameof(TransportFailures))]
    public async Task Transport_failures_are_translated(Exception thrown, BookSourceFailure expected)
    {
        var ex = await Should.ThrowAsync<BookSourceException>(
            () => CreateClient(StubHttpHandler.Throws(thrown)).GetBookOwnersAsync(CancellationToken.None));

        ex.Failure.ShouldBe(expected);
        ex.InnerException.ShouldBeSameAs(thrown);
    }

    [Fact]
    public async Task An_open_circuit_tells_the_caller_when_to_retry()
    {
        var ex = await Should.ThrowAsync<BookSourceException>(
            () => CreateClient(StubHttpHandler.Throws(new BrokenCircuitException("open"))).GetBookOwnersAsync(CancellationToken.None));

        ex.RetryAfter.ShouldBe(BupaApiClient.CircuitBreakDuration);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_reported_as_an_upstream_failure()
    {
        using var cts = new CancellationTokenSource();
        var handler = new StubHttpHandler((_, token) =>
        {
            cts.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult(StubHttpHandler.JsonResponse("[]"));
        });

        await Should.ThrowAsync<OperationCanceledException>(() => CreateClient(handler).GetBookOwnersAsync(cts.Token));
    }
}
