using BupaBooks.Infrastructure.BupaApi;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BupaBooks.Infrastructure.Tests;

/// <summary>
/// Calls the REAL Bupa API to detect upstream contract drift (the Swagger document
/// declares no schema, so this test is the contract). Excluded from PR builds
/// (network-dependent); run on a schedule in CI:
/// <c>dotnet test --filter Category=LiveContract</c>.
/// </summary>
[Trait("Category", "LiveContract")]
public class LiveUpstreamContractTests
{
    [Fact]
    public async Task Upstream_still_matches_the_expected_shape()
    {
        var options = new BupaApiOptions();
        using var http = new HttpClient { BaseAddress = options.BaseUrl, Timeout = TimeSpan.FromSeconds(20) };
        var client = new BupaApiClient(http, Options.Create(options), NullLogger<BupaApiClient>.Instance);

        var owners = await client.GetBookOwnersAsync(CancellationToken.None);

        owners.ShouldNotBeEmpty("upstream returned no valid owners; every record may have been rejected");
        owners.SelectMany(owner => owner.Books!)
            .Select(book => BupaOwnerMapper.ToBookFormat(book.Type!))
            .ShouldAllBe(format => format != Application.Domain.BookFormat.Other, "upstream introduced an unknown book type");
    }
}
