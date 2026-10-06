namespace BupaBooks.Application.Domain;

/// <summary>
/// Physical or digital format of a book. <see cref="Other"/> exists so an unknown
/// upstream format degrades gracefully instead of failing the whole request.
/// </summary>
public enum BookFormat
{
    Hardcover,
    Paperback,
    Ebook,
    Other,
}
