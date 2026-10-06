namespace BupaBooks.Application.Domain;

/// <summary>Opaque identifier of one owned copy of a book.</summary>
public sealed record BookId
{
    public string Value { get; }

    private BookId(string value) => Value = value;

    public static BookId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new BookId(value);
    }

    public override string ToString() => Value;
}
