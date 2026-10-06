using BupaBooks.Application.Abstractions;
using BupaBooks.Application.Domain;
using BupaBooks.Application.Paging;

namespace BupaBooks.Application.Books.GetBooks;

/// <summary>
/// Use case: return one page of books owned by a given age category, optionally
/// filtered by format, in alphabetical order. A plain class rather than a
/// mediator handler: one use case does not justify the indirection.
/// </summary>
public sealed class GetBooksHandler(IBookOwnerSource source)
{
    public async Task<Page<BookSummary>> HandleAsync(GetBooksQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var owners = await source.GetOwnersAsync(cancellationToken).ConfigureAwait(false);
        var books = BookSelection.Select(owners, query.Criteria);

        return Page.Create(books, query.Page, book => new BookSummary(book.Id.Value, book.Title, book.Format));
    }
}
