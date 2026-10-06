using System.Text.Json.Serialization;

namespace BupaBooks.Infrastructure.BupaApi.Dtos;

/// <summary>
/// The upstream JSON shape, exactly as Bupa sends it. Everything is nullable on
/// purpose: we do not trust a third party's payload until the reader validates it.
/// <para>
/// <see cref="Id"/> is an assumption: the brief says the structure is "similar to"
/// the example, so production data is assumed to carry ids. The test endpoint does
/// not, so ids are optional and derived when absent (see <see cref="DerivedId"/>).
/// </para>
/// </summary>
public sealed record BupaBookOwnerDto(
    [property: JsonConverter(typeof(FlexibleIdConverter))] string? Id,
    string? Name,
    int? Age,
    BupaBookDto[]? Books);

public sealed record BupaBookDto(
    [property: JsonConverter(typeof(FlexibleIdConverter))] string? Id,
    string? Name,
    string? Type);
