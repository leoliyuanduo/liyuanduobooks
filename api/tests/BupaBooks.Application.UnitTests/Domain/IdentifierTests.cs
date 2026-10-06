using BupaBooks.Application.Domain;

namespace BupaBooks.Application.UnitTests.Domain;

public class IdentifierTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_ids_are_rejected(string value)
    {
        Should.Throw<ArgumentException>(() => OwnerId.From(value));
        Should.Throw<ArgumentException>(() => BookId.From(value));
    }

    [Fact]
    public void Ids_are_compared_by_value() =>
        BookId.From("42").ShouldBe(BookId.From("42"));

    [Fact]
    public void Owner_and_book_reject_blank_names()
    {
        Should.Throw<ArgumentException>(() => new OwnedBook(BookId.From("b"), " ", BookFormat.Ebook));
        Should.Throw<ArgumentException>(() => new Owner(OwnerId.From("o"), "", Age.FromYears(30), []));
    }
}
