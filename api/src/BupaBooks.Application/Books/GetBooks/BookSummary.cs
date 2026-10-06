using BupaBooks.Application.Domain;

namespace BupaBooks.Application.Books.GetBooks;

/// <summary>What the use case returns per book: no owner data, by design (data minimisation).</summary>
public sealed record BookSummary(string Id, string Title, BookFormat Format);
