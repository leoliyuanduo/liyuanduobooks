using BupaBooks.Application.Domain;
using BupaBooks.Infrastructure.BupaApi;
using BupaBooks.Infrastructure.BupaApi.Dtos;

namespace BupaBooks.Infrastructure.Tests;

/// <summary>The anti-corruption layer: upstream vocabulary in, domain model out.</summary>
public class BupaOwnerMapperTests
{
    [Theory]
    [InlineData("Hardcover", BookFormat.Hardcover)]
    [InlineData("HARDCOVER", BookFormat.Hardcover)]
    [InlineData(" hardcover ", BookFormat.Hardcover)]
    [InlineData("Paperback", BookFormat.Paperback)]
    [InlineData("Ebook", BookFormat.Ebook)]
    [InlineData("Audiobook", BookFormat.Other)]
    [InlineData("", BookFormat.Other)]
    public void Maps_upstream_book_types(string upstream, BookFormat expected) =>
        BupaOwnerMapper.ToBookFormat(upstream).ShouldBe(expected);

    [Fact]
    public void Uses_upstream_ids_when_present()
    {
        var dto = new BupaBookOwnerDto("owner-7", "Jane", 23, [new BupaBookDto("42", "Hamlet", "Hardcover")]);

        var owner = BupaOwnerMapper.ToDomain([dto]).Single();

        owner.Id.Value.ShouldBe("owner-7");
        owner.Books.Single().Id.Value.ShouldBe("42");
        owner.Category.ShouldBe(AgeCategory.Adult);
    }

    [Fact]
    public void Derives_deterministic_unique_ids_when_upstream_has_none()
    {
        BupaBookOwnerDto[] dtos =
        [
            new(null, "Ann", 40, [new(null, "Hamlet", "Hardcover"), new(null, "Hamlet", "Hardcover")]),
            new(null, "Ann", 40, [new(null, "Hamlet", "Hardcover")]),
        ];

        var first = BupaOwnerMapper.ToDomain(dtos);
        var second = BupaOwnerMapper.ToDomain(dtos);

        var firstIds = first.SelectMany(owner => owner.Books).Select(book => book.Id.Value).ToArray();
        var secondIds = second.SelectMany(owner => owner.Books).Select(book => book.Id.Value).ToArray();

        firstIds.ShouldBe(secondIds); // stable across calls: caching, ETags and paging rely on it
        firstIds.Distinct().Count().ShouldBe(3); // identical-looking copies still get distinct ids
        first.Select(owner => owner.Id).Distinct().Count().ShouldBe(2);
    }
}
