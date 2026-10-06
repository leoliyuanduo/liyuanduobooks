using BupaBooks.Application.Abstractions;
using BupaBooks.Application.Books.GetBooks;
using BupaBooks.Application.Domain;
using BupaBooks.Application.Paging;

namespace BupaBooks.Application.UnitTests.Books;

public class GetBooksHandlerTests
{
    /// <summary>Hand-written fake: one port, one method, no mocking library needed.</summary>
    private sealed class FakeSource(Func<IReadOnlyList<Owner>> owners) : IBookOwnerSource
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<Owner>> GetOwnersAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(owners());
        }
    }

    [Fact]
    public async Task Returns_a_page_of_book_summaries_without_owner_data()
    {
        var handler = new GetBooksHandler(new FakeSource(TestOwners.Live));
        var query = new GetBooksQuery(new BookCriteria(AgeCategory.Child, BookFormat.Hardcover), new PageRequest(50, 0));

        var page = await handler.HandleAsync(query, CancellationToken.None);

        page.Items.Select(book => book.Title).ShouldBe(["Great Expectations", "Little Red Riding Hood"]);
        page.Items.ShouldAllBe(book => book.Format == BookFormat.Hardcover);
        page.TotalCount.ShouldBe(2);
        page.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task Paging_walks_the_sorted_list_without_gaps_or_duplicates()
    {
        var handler = new GetBooksHandler(new FakeSource(TestOwners.Live));
        var criteria = new BookCriteria(AgeCategory.Adult);
        var seen = new List<string>();
        var offset = 0;

        while (true)
        {
            var page = await handler.HandleAsync(new GetBooksQuery(criteria, new PageRequest(4, offset)), CancellationToken.None);
            seen.AddRange(page.Items.Select(book => book.Title));
            if (page.NextCursor is null || !Cursor.TryDecode(page.NextCursor, out offset))
            {
                break;
            }
        }

        seen.ShouldBe(
        [
            "Great Expectations", "Gulliver's Travels", "Hamlet", "Jane Eyre", "React: The Ultimate Guide", "Wuthering Heights",
        ]);
    }

    [Fact]
    public async Task Source_failures_propagate_unchanged()
    {
        var failure = new BookSourceException(BookSourceFailure.Timeout, "slow");
        var handler = new GetBooksHandler(new FakeSource(() => throw failure));
        var query = new GetBooksQuery(new BookCriteria(AgeCategory.Adult), new PageRequest(50, 0));

        var thrown = await Should.ThrowAsync<BookSourceException>(() => handler.HandleAsync(query, CancellationToken.None));
        thrown.ShouldBeSameAs(failure);
    }
}
