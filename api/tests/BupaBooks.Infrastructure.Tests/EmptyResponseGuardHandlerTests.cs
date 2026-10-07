using System.Net;
using BupaBooks.Infrastructure.BupaApi;

namespace BupaBooks.Infrastructure.Tests;

public class EmptyResponseGuardHandlerTests
{
    private static HttpClient ClientOver(StubHttpHandler network) =>
        new(new EmptyResponseGuardHandler { InnerHandler = network }) { BaseAddress = new Uri("https://upstream.test/") };

    [Theory]
    [InlineData("")]
    [InlineData("   \r\n")]
    public async Task An_empty_success_is_a_transient_failure(string body)
    {
        using var client = ClientOver(StubHttpHandler.Json(body));

        var ex = await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync("x"));

        ex.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_non_empty_success_passes_through_and_stays_readable()
    {
        using var client = ClientOver(StubHttpHandler.Json("[]"));

        using var response = await client.GetAsync("x");

        (await response.Content.ReadAsStringAsync()).ShouldBe("[]");
    }

    [Fact]
    public async Task Error_statuses_are_left_to_the_retry_policy()
    {
        using var client = ClientOver(StubHttpHandler.Json("", HttpStatusCode.TooManyRequests));

        using var response = await client.GetAsync("x");

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
