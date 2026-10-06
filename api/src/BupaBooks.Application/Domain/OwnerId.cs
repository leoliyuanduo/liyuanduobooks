namespace BupaBooks.Application.Domain;

/// <summary>Opaque identifier of a book owner. Its format is owned by the upstream system.</summary>
public sealed record OwnerId
{
    public string Value { get; }

    private OwnerId(string value) => Value = value;

    public static OwnerId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new OwnerId(value);
    }

    public override string ToString() => Value;
}
