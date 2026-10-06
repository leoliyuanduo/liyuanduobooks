using BupaBooks.Application.Domain;
using static BupaBooks.Application.UnitTests.TestOwners;

namespace BupaBooks.Application.UnitTests.Domain;

public class BookSelectionTests
{
    private static string[] Titles(IEnumerable<OwnedBook> books) => books.Select(book => book.Title).ToArray();

    [Fact]
    public void Adults_see_all_their_books_alphabetically() =>
        Titles(BookSelection.Select(Live(), new BookCriteria(AgeCategory.Adult))).ShouldBe(
        [
            "Great Expectations",
            "Gulliver's Travels",
            "Hamlet",
            "Jane Eyre",
            "React: The Ultimate Guide",
            "Wuthering Heights",
        ]);

    [Fact]
    public void Children_see_all_their_books_alphabetically() =>
        Titles(BookSelection.Select(Live(), new BookCriteria(AgeCategory.Child))).ShouldBe(
            ["Great Expectations", "Hamlet", "Little Red Riding Hood", "The Hobbit"]);

    [Fact]
    public void Hardcover_filter_applies_within_the_category()
    {
        Titles(BookSelection.Select(Live(), new BookCriteria(AgeCategory.Adult, BookFormat.Hardcover))).ShouldBe(
            ["Great Expectations", "Gulliver's Travels", "Hamlet", "React: The Ultimate Guide"]);

        Titles(BookSelection.Select(Live(), new BookCriteria(AgeCategory.Child, BookFormat.Hardcover))).ShouldBe(
            ["Great Expectations", "Little Red Riding Hood"]);
    }

    [Fact]
    public void A_seventeen_year_old_is_a_child_and_an_eighteen_year_old_is_an_adult()
    {
        var owners = new[]
        {
            AnOwner("Seventeen", 17, ABook("Child Book", BookFormat.Ebook)),
            AnOwner("Eighteen", 18, ABook("Adult Book", BookFormat.Ebook)),
        };

        Titles(BookSelection.Select(owners, new BookCriteria(AgeCategory.Child))).ShouldBe(["Child Book"]);
        Titles(BookSelection.Select(owners, new BookCriteria(AgeCategory.Adult))).ShouldBe(["Adult Book"]);
    }

    [Fact]
    public void Sorting_is_case_insensitive_and_numeric()
    {
        var owners = new[]
        {
            AnOwner("A", 30,
                ABook("zebra", BookFormat.Ebook),
                ABook("Book 10", BookFormat.Ebook),
                ABook("apple", BookFormat.Ebook),
                ABook("Book 2", BookFormat.Ebook)),
        };

        Titles(BookSelection.Select(owners, new BookCriteria(AgeCategory.Adult))).ShouldBe(
            ["apple", "Book 2", "Book 10", "zebra"]);
    }

    [Fact]
    public void Every_owned_copy_is_listed_and_ties_are_broken_by_id()
    {
        var owners = new[]
        {
            AnOwner("Ann", 40, ABook("Hamlet", BookFormat.Hardcover, id: "b-2")),
            AnOwner("Bob", 50, ABook("Hamlet", BookFormat.Hardcover, id: "b-1")),
        };

        BookSelection.Select(owners, new BookCriteria(AgeCategory.Adult))
            .Select(book => book.Id.Value)
            .ShouldBe(["b-1", "b-2"]);
    }

    [Fact]
    public void No_owners_means_no_books() =>
        BookSelection.Select([], new BookCriteria(AgeCategory.Adult)).ShouldBeEmpty();
}
