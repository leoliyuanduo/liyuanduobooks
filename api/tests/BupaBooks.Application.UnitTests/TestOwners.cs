using BupaBooks.Application.Domain;

namespace BupaBooks.Application.UnitTests;

/// <summary>Test data builders. Mirrors the live API payload (6 Oct 2026) in domain form.</summary>
internal static class TestOwners
{
    private static int _nextId;

    public static OwnedBook ABook(string title, BookFormat format, string? id = null) =>
        new(BookId.From(id ?? $"b{Interlocked.Increment(ref _nextId)}"), title, format);

    public static Owner AnOwner(string name, int age, params OwnedBook[] books) =>
        new(OwnerId.From($"o-{name}"), name, Age.FromYears(age), books);

    public static IReadOnlyList<Owner> Live() =>
    [
        AnOwner("Jane", 23, ABook("Hamlet", BookFormat.Hardcover), ABook("Wuthering Heights", BookFormat.Paperback)),
        AnOwner("Charlotte", 14, ABook("Hamlet", BookFormat.Paperback)),
        AnOwner("Max", 25,
            ABook("React: The Ultimate Guide", BookFormat.Hardcover),
            ABook("Gulliver's Travels", BookFormat.Hardcover),
            ABook("Jane Eyre", BookFormat.Paperback),
            ABook("Great Expectations", BookFormat.Hardcover)),
        AnOwner("William", 15, ABook("Great Expectations", BookFormat.Hardcover)),
        AnOwner("Charles", 17, ABook("Little Red Riding Hood", BookFormat.Hardcover), ABook("The Hobbit", BookFormat.Ebook)),
    ];
}
