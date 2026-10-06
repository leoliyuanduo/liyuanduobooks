using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BupaBooks.Application.Abstractions;
using BupaBooks.Application.Paging;

namespace BupaBooks.Host.IntegrationTests;

public class BooksEndpointTests : IDisposable
{
    private readonly BooksApiFactory _factory = new();
    private readonly HttpClient _client;

    public BooksEndpointTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string[] Titles(JsonElement body) =>
        body.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("title").GetString()!).ToArray();

    [Fact]
    public async Task Returns_adult_books_alphabetically()
    {
        var response = await _client.GetAsync("/api/v1/books?ageCategory=adult");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Titles(body).ShouldBe(
        [
            "Great Expectations", "Gulliver's Travels", "Hamlet", "Jane Eyre", "React: The Ultimate Guide", "Wuthering Heights",
        ]);
        body.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldBe(6);
        body.GetProperty("page").GetProperty("nextCursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Returns_hardcover_books_owned_by_children_without_owner_data()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>("/api/v1/books?ageCategory=child&format=hardcover");

        Titles(body).ShouldBe(["Great Expectations", "Little Red Riding Hood"]);

        var first = body.GetProperty("items")[0];
        first.GetProperty("format").GetString().ShouldBe("hardcover"); // camelCase string, not a number
        first.GetProperty("id").GetString().ShouldBe("b8");
        first.EnumerateObject().Select(property => property.Name).ShouldBe(["id", "title", "format"]); // no name/age
    }

    [Fact]
    public async Task Paging_with_cursors_returns_every_book_exactly_once()
    {
        var titles = new List<string>();
        var url = "/api/v1/books?ageCategory=adult&limit=4";

        while (true)
        {
            var body = await _client.GetFromJsonAsync<JsonElement>(url);
            titles.AddRange(Titles(body));
            var next = body.GetProperty("page").GetProperty("nextCursor");
            if (next.ValueKind == JsonValueKind.Null)
            {
                break;
            }

            url = $"/api/v1/books?ageCategory=adult&limit=4&cursor={Uri.EscapeDataString(next.GetString()!)}";
        }

        titles.Count.ShouldBe(6);
        titles.Distinct().Count().ShouldBe(6);
    }

    [Fact]
    public async Task Invalid_parameters_return_every_error_as_problem_details()
    {
        var response = await _client.GetAsync("/api/v1/books?format=audiobook&limit=500&cursor=garbage!");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");

        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.EnumerateObject().Select(error => error.Name).Order().ShouldBe(["ageCategory", "cursor", "format", "limit"]);
    }

    [Theory]
    [InlineData("ageCategory=1")] // enum numbers are not accepted
    [InlineData("ageCategory=teen")]
    [InlineData("ageCategory=adult&limit=0")]
    [InlineData("ageCategory=adult&limit=-5")]
    public async Task Rejects_values_outside_the_contract(string query) =>
        (await _client.GetAsync($"/api/v1/books?{query}")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    [Fact]
    public async Task Supports_conditional_requests_with_etags()
    {
        var first = await _client.GetAsync("/api/v1/books?ageCategory=adult");
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();
        first.Headers.CacheControl!.MaxAge.ShouldBe(TimeSpan.FromSeconds(60));

        using var conditional = new HttpRequestMessage(HttpMethod.Get, "/api/v1/books?ageCategory=adult");
        conditional.Headers.IfNoneMatch.Add(etag);
        var second = await _client.SendAsync(conditional);

        second.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await second.Content.ReadAsByteArrayAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Different_queries_have_different_etags()
    {
        var adults = await _client.GetAsync("/api/v1/books?ageCategory=adult");
        var children = await _client.GetAsync("/api/v1/books?ageCategory=child");

        adults.Headers.ETag.ShouldNotBe(children.Headers.ETag);
    }

    [Theory]
    [InlineData(BookSourceFailure.Timeout, HttpStatusCode.GatewayTimeout)]
    [InlineData(BookSourceFailure.Unavailable, HttpStatusCode.ServiceUnavailable)]
    [InlineData(BookSourceFailure.UpstreamError, HttpStatusCode.BadGateway)]
    [InlineData(BookSourceFailure.InvalidResponse, HttpStatusCode.BadGateway)]
    public async Task Upstream_failures_map_to_gateway_problem_details(BookSourceFailure failure, HttpStatusCode expected)
    {
        _factory.Source.Behaviour = () => throw new BookSourceException(failure, "internal detail that must not leak");

        var response = await _client.GetAsync("/api/v1/books?ageCategory=adult");

        response.StatusCode.ShouldBe(expected);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var raw = await response.Content.ReadAsStringAsync();
        raw.ShouldNotContain("internal detail");
        JsonDocument.Parse(raw).RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task An_open_circuit_returns_retry_after()
    {
        _factory.Source.Behaviour = () =>
            throw new BookSourceException(BookSourceFailure.Unavailable, "open", retryAfter: TimeSpan.FromSeconds(15));

        var response = await _client.GetAsync("/api/v1/books?ageCategory=adult");

        response.Headers.RetryAfter!.Delta.ShouldBe(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public async Task A_cursor_past_the_end_returns_an_empty_last_page()
    {
        var body = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/books?ageCategory=adult&cursor={Cursor.Encode(50)}");

        Titles(body).ShouldBeEmpty();
        body.GetProperty("page").GetProperty("nextCursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }
}
