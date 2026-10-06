namespace BupaBooks.Application.Domain;

/// <summary>One copy of a book owned by someone. An entity: two copies of "Hamlet" are different books.</summary>
public sealed class OwnedBook
{
    public BookId Id { get; }
    public string Title { get; }
    public BookFormat Format { get; }

    public OwnedBook(BookId id, string title, BookFormat format)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Id = id;
        Title = title.Trim();
        Format = format;
    }
}
