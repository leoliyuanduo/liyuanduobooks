namespace BupaBooks.Infrastructure.BupaApi.Dtos;

/// <summary>
/// The upstream JSON shape, exactly as Bupa sends it (verified against the live data,
/// the Swagger page and the reference screenshots: there are no identifiers).
/// Everything is nullable on purpose: we do not trust a third party's payload until
/// the reader validates it.
/// </summary>
public sealed record BupaBookOwnerDto(string? Name, int? Age, BupaBookDto[]? Books);

public sealed record BupaBookDto(string? Name, string? Type);
