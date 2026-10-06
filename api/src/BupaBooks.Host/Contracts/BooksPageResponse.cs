namespace BupaBooks.Host.Contracts;

/// <summary>Response body of GET /api/v1/books. This type IS the public contract (it defines the OpenAPI schema).</summary>
public sealed record BooksPageResponse(IReadOnlyList<BookResponse> Items, PageInfo Page);

/// <summary>One owned copy of a book. <see cref="Id"/> is opaque: store it, never parse it.</summary>
public sealed record BookResponse(string Id, string Title, BookFormatContract Format);

/// <summary>Paging metadata. Pass <see cref="NextCursor"/> back as the cursor query parameter; null means last page.</summary>
public sealed record PageInfo(int Limit, string? NextCursor, int TotalCount);

/// <summary>
/// Book format as exposed by the API (serialised camelCase: "hardcover").
/// Deliberately separate from the domain enum so the domain can evolve without breaking clients.
/// </summary>
public enum BookFormatContract
{
    Hardcover,
    Paperback,
    Ebook,
    Other,
}
