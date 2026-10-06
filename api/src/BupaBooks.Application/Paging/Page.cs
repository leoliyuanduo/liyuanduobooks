namespace BupaBooks.Application.Paging;

/// <summary>One page of results plus what the caller needs to fetch the next one.</summary>
public sealed record Page<T>(IReadOnlyList<T> Items, int Limit, string? NextCursor, int TotalCount);

public static class Page
{
    /// <summary>Slices an already-sorted list. Sorting must happen before paging, or pages reshuffle.</summary>
    public static Page<TResult> Create<TSource, TResult>(
        IReadOnlyList<TSource> sortedItems,
        PageRequest request,
        Func<TSource, TResult> map)
    {
        ArgumentNullException.ThrowIfNull(sortedItems);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(map);

        var items = sortedItems.Skip(request.Offset).Take(request.Limit).Select(map).ToArray();
        var nextOffset = request.Offset + request.Limit;
        var nextCursor = nextOffset < sortedItems.Count ? Cursor.Encode(nextOffset) : null;

        return new Page<TResult>(items, request.Limit, nextCursor, sortedItems.Count);
    }
}
