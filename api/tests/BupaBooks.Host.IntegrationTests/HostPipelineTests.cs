using System.Net;
using System.Text.Json;

namespace BupaBooks.Host.IntegrationTests;

public class HostPipelineTests(BooksApiFactory factory) : IClassFixture<BooksApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Liveness_does_not_depend_on_upstream() =>
        (await _client.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);

    [Fact]
    public async Task Unknown_api_routes_are_problem_details_not_the_spa()
    {
        var response = await _client.GetAsync("/api/v1/nope");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Echoes_a_valid_correlation_id()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", "abc-123");

        var response = await _client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-ID").Single().ShouldBe("abc-123");
    }

    [Fact]
    public async Task Replaces_an_unsafe_correlation_id()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", "<script>alert(1)</script>");

        var response = await _client.SendAsync(request);

        var echoed = response.Headers.GetValues("X-Correlation-ID").Single();
        Guid.TryParse(echoed, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Publishes_an_openapi_document_describing_the_books_endpoint()
    {
        var document = JsonDocument.Parse(await _client.GetStringAsync("/openapi/v1.json")).RootElement;

        document.GetProperty("paths").TryGetProperty("/api/v1/books", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task OpenApi_publishes_the_real_query_contract_for_client_code_generation()
    {
        var document = JsonDocument.Parse(await _client.GetStringAsync("/openapi/v1.json")).RootElement;
        var parameters = document.GetProperty("paths").GetProperty("/api/v1/books").GetProperty("get")
            .GetProperty("parameters").EnumerateArray()
            .ToDictionary(parameter => parameter.GetProperty("name").GetString()!);

        var ageCategory = parameters["ageCategory"];
        ageCategory.GetProperty("required").GetBoolean().ShouldBeTrue();
        ageCategory.GetProperty("schema").GetProperty("enum").EnumerateArray()
            .Select(value => value.GetString()).ShouldBe(["adult", "child"]);

        parameters["format"].GetProperty("schema").GetProperty("enum").EnumerateArray()
            .Select(value => value.GetString()).ShouldBe(["hardcover", "paperback", "ebook"]);

        parameters["limit"].GetProperty("schema").GetProperty("type").GetString().ShouldBe("integer");
    }

    [Fact]
    public async Task OpenApi_types_page_numbers_as_integers_only()
    {
        var document = JsonDocument.Parse(await _client.GetStringAsync("/openapi/v1.json")).RootElement;
        var totalCount = document.GetProperty("components").GetProperty("schemas")
            .GetProperty("PageInfo").GetProperty("properties").GetProperty("totalCount");

        totalCount.GetProperty("type").GetString().ShouldBe("integer");
    }
}
