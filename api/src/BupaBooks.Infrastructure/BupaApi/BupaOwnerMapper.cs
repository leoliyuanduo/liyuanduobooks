using BupaBooks.Application.Domain;
using BupaBooks.Infrastructure.BupaApi.Dtos;

namespace BupaBooks.Infrastructure.BupaApi;

/// <summary>
/// Anti-corruption layer: translates Bupa's vocabulary ("name", "type": "Hardcover")
/// into our domain model. If the upstream shape changes, this is the only place
/// that changes. Input must come from <see cref="BupaPayloadReader"/>.
/// </summary>
internal static class BupaOwnerMapper
{
    public static IReadOnlyList<Owner> ToDomain(IReadOnlyList<BupaBookOwnerDto> owners)
    {
        ArgumentNullException.ThrowIfNull(owners);

        var result = new Owner[owners.Count];
        for (var index = 0; index < owners.Count; index++)
        {
            result[index] = ToDomain(owners[index], index);
        }

        return result;
    }

    private static Owner ToDomain(BupaBookOwnerDto dto, int index)
    {
        var name = dto.Name!;
        var age = dto.Age!.Value;
        var ownerId = DerivedId.ForOwner(index, name, age);

        var books = (dto.Books ?? []).Select((book, bookIndex) =>
        {
            var title = book.Name!;
            var type = book.Type ?? string.Empty;
            var bookId = DerivedId.ForBook(ownerId, bookIndex, title, type);
            return new OwnedBook(BookId.From(bookId), title, ToBookFormat(type));
        });

        return new Owner(OwnerId.From(ownerId), name, Age.FromYears(age), books);
    }

    internal static BookFormat ToBookFormat(string upstreamType) =>
        upstreamType.Trim().ToUpperInvariant() switch
        {
            "HARDCOVER" => BookFormat.Hardcover,
            "PAPERBACK" => BookFormat.Paperback,
            "EBOOK" => BookFormat.Ebook,
            _ => BookFormat.Other,
        };
}
