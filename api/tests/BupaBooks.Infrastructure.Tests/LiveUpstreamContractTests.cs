using BupaBooks.Infrastructure.BupaApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BupaBooks.Infrastructure.Tests;

/// <summary>
/// Calls the REAL Bupa API to detect upstream contract drift (the Swagger document
/// declares no schema, so this test is the contract). Excluded from PR builds
/// (network-dependent); run on a schedule in CI:
/// <c>dotnet test --filter Category=LiveContract</c>.
/// <para>
/// It uses the production pipeline from <c>AddInfrastructure</c> (retries with backoff,
/// the empty-response guard), because the real API is rate-limited (429) and
/// intermittently returns empty bodies. A bare HttpClient would fail on that known
/// behaviour rather than on a contract change, which is the thing this test is for.
/// </para>
/// </summary>
[Trait("Category", "LiveContract")]
public class LiveUpstreamContractTests
{
    [Fact]
    public async Task Upstream_still_matches_the_expected_shape()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(new ConfigurationBuilder().Build()); // default options: the real Bupa URL
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<BupaApiClient>();

        var owners = await client.GetBookOwnersAsync(CancellationToken.None);

        owners.ShouldNotBeEmpty("upstream returned no valid owners; every record may have been rejected");
        owners.SelectMany(owner => owner.Books!)
            .Select(book => BupaOwnerMapper.ToBookFormat(book.Type!))
            .ShouldAllBe(format => format != Application.Domain.BookFormat.Other, "upstream introduced an unknown book type");
    }
}
