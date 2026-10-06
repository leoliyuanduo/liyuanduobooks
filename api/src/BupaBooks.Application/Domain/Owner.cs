namespace BupaBooks.Application.Domain;

/// <summary>A person who owns books. Their name and age never leave the backend.</summary>
public sealed class Owner
{
    public OwnerId Id { get; }
    public string Name { get; }
    public Age Age { get; }
    public IReadOnlyList<OwnedBook> Books { get; }

    public AgeCategory Category => Age.Category;

    public Owner(OwnerId id, string name, Age age, IEnumerable<OwnedBook> books)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(books);

        Id = id;
        Name = name.Trim();
        Age = age;
        Books = books.ToArray();
    }
}
