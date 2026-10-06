using BupaBooks.Application.Books.GetBooks;
using BupaBooks.Application.Domain;
using BupaBooks.Application.Paging;

namespace BupaBooks.Host.Contracts;

/// <summary>Use-case result to wire contract. Explicit mapping: easy to read, easy to debug.</summary>
internal static class Mapping
{
    public static BooksPageResponse ToResponse(this Page<BookSummary> page) =>
        new(
            page.Items.Select(book => new BookResponse(book.Id, book.Title, book.Format.ToContract())).ToArray(),
            new PageInfo(page.Limit, page.NextCursor, page.TotalCount));

    private static BookFormatContract ToContract(this BookFormat format) =>
        format switch
        {
            BookFormat.Hardcover => BookFormatContract.Hardcover,
            BookFormat.Paperback => BookFormatContract.Paperback,
            BookFormat.Ebook => BookFormatContract.Ebook,
            BookFormat.Other => BookFormatContract.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unmapped book format."),
        };
}
