namespace BupaBooks.Application.Domain;

/// <summary>
/// Domain service holding the core rule of the challenge: books are grouped by
/// the age category of their owner, optionally filtered by format, and sorted
/// alphabetically. Pure and deterministic.
/// </summary>
public static class BookSelection
{
    public static IReadOnlyList<OwnedBook> Select(IEnumerable<Owner> owners, BookCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(owners);
        ArgumentNullException.ThrowIfNull(criteria);

        return owners
            .Where(owner => owner.Category == criteria.AgeCategory)
            .SelectMany(owner => owner.Books)
            .Where(book => criteria.Format is null || book.Format == criteria.Format)
            .Order(BookOrdering.Instance)
            .ToArray();
    }
}
