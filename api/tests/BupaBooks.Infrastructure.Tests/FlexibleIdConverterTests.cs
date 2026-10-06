using System.Text.Json;
using BupaBooks.Infrastructure.BupaApi.Dtos;

namespace BupaBooks.Infrastructure.Tests;

/// <summary>The upstream id format is unknown (assumption), so both strings and numbers are accepted.</summary>
public class FlexibleIdConverterTests
{
    [Theory]
    [InlineData("""{ "id": "a1b2", "name": "Hamlet", "type": "Hardcover" }""", "a1b2")]
    [InlineData("""{ "id": 42, "name": "Hamlet", "type": "Hardcover" }""", "42")]
    [InlineData("""{ "id": null, "name": "Hamlet", "type": "Hardcover" }""", null)]
    [InlineData("""{ "name": "Hamlet", "type": "Hardcover" }""", null)]
    public void Reads_ids_as_opaque_strings(string json, string? expected) =>
        JsonSerializer.Deserialize<BupaBookDto>(json, JsonSerializerOptions.Web)!.Id.ShouldBe(expected);

    [Fact]
    public void Rejects_ids_that_are_objects() =>
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<BupaBookDto>("""{ "id": { "x": 1 }, "name": "Hamlet" }""", JsonSerializerOptions.Web));
}
