using System.Net;
using System.Text.Json;
using BupaBooks.Host.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BupaBooks.Host.IntegrationTests;

public class EdgeProtectionTests
{
    [Fact]
    public async Task Clients_over_the_rate_limit_get_429_problem_details_with_retry_after()
    {
        await using var factory = new BooksApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.Configure<ClientRateLimitOptions>(options =>
            {
                options.PermitLimit = 2;
                options.TokensPerPeriod = 1;
                options.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
            })));
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/v1/books?ageCategory=adult")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/v1/books?ageCategory=child")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var rejected = await client.GetAsync("/api/v1/books?ageCategory=adult");

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        rejected.Headers.RetryAfter.ShouldNotBeNull();
        JsonDocument.Parse(await rejected.Content.ReadAsStringAsync()).RootElement
            .GetProperty("status").GetInt32().ShouldBe(429);
    }

    [Fact]
    public async Task Health_checks_are_not_rate_limited()
    {
        await using var factory = new BooksApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.Configure<ClientRateLimitOptions>(options => options.PermitLimit = 1)));
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            (await client.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Responses_carry_browser_security_headers()
    {
        await using var factory = new BooksApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/books?ageCategory=adult");

        response.Headers.GetValues("X-Content-Type-Options").Single().ShouldBe("nosniff");
        response.Headers.GetValues("X-Frame-Options").Single().ShouldBe("DENY");
        response.Headers.GetValues("Referrer-Policy").Single().ShouldBe("no-referrer");
        response.Headers.GetValues("Content-Security-Policy").Single().ShouldContain("frame-ancestors 'none'");
    }

    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")] // IPv4-mapped IPv6 is the same client
    [InlineData("2001:db8:abcd:12:1:2:3:4", "2001:db8:abcd:12::/64")] // IPv6 grouped by /64
    [InlineData("2001:db8:abcd:12:ffff:ffff:ffff:ffff", "2001:db8:abcd:12::/64")]
    public void Clients_are_partitioned_by_ip(string remoteIp, string expectedKey)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);

        RateLimitingSetup.ClientKey(context).ShouldBe(expectedKey);
    }

    [Fact]
    public void A_missing_remote_address_shares_one_partition() =>
        RateLimitingSetup.ClientKey(new DefaultHttpContext()).ShouldBe("unknown");
}
