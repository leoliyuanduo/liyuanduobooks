using System.Globalization;

namespace BupaBooks.Application.Domain;

/// <summary>
/// "Alphabetical order" as a business rule: Australian English, case-insensitive,
/// numbers compared by value ("Book 2" before "Book 10"). Ties are broken by id
/// so the order is deterministic, which paging depends on.
/// </summary>
public sealed class BookOrdering : IComparer<OwnedBook>
{
    public static readonly BookOrdering Instance = new();

    private static readonly StringComparer TitleComparer = StringComparer.Create(
        CultureInfo.GetCultureInfo("en-AU"),
        CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);

    private BookOrdering()
    {
    }

    public int Compare(OwnedBook? x, OwnedBook? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var byTitle = TitleComparer.Compare(x.Title, y.Title);
        return byTitle != 0 ? byTitle : string.CompareOrdinal(x.Id.Value, y.Id.Value);
    }
}
