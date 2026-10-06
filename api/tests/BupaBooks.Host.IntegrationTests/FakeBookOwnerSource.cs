using BupaBooks.Application.Abstractions;
using BupaBooks.Application.Domain;

namespace BupaBooks.Host.IntegrationTests;

/// <summary>Stands in for the upstream API so the HTTP pipeline can be tested in isolation.</summary>
public sealed class FakeBookOwnerSource : IBookOwnerSource
{
    public Func<IReadOnlyList<Owner>> Behaviour { get; set; } = Live;

    public Task<IReadOnlyList<Owner>> GetOwnersAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Behaviour());

    /// <summary>The live API data (6 Oct 2026) in domain form, with upstream-style ids.</summary>
    public static IReadOnlyList<Owner> Live() =>
    [
        Owner("o1", "Jane", 23, ("b1", "Hamlet", BookFormat.Hardcover), ("b2", "Wuthering Heights", BookFormat.Paperback)),
        Owner("o2", "Charlotte", 14, ("b3", "Hamlet", BookFormat.Paperback)),
        Owner("o3", "Max", 25,
            ("b4", "React: The Ultimate Guide", BookFormat.Hardcover),
            ("b5", "Gulliver's Travels", BookFormat.Hardcover),
            ("b6", "Jane Eyre", BookFormat.Paperback),
            ("b7", "Great Expectations", BookFormat.Hardcover)),
        Owner("o4", "William", 15, ("b8", "Great Expectations", BookFormat.Hardcover)),
        Owner("o5", "Charles", 17, ("b9", "Little Red Riding Hood", BookFormat.Hardcover), ("b10", "The Hobbit", BookFormat.Ebook)),
    ];

    private static Owner Owner(string id, string name, int age, params (string Id, string Title, BookFormat Format)[] books) =>
        new(
            OwnerId.From(id),
            name,
            Age.FromYears(age),
            books.Select(book => new OwnedBook(BookId.From(book.Id), book.Title, book.Format)));
}
