namespace BupaBooks.Application.Paging;

/// <summary>A validated request for one page of results.</summary>
public sealed record PageRequest
{
    public const int MinLimit = 1;
    public const int MaxLimit = 100;
    public const int DefaultLimit = 50;

    public int Limit { get; }
    public int Offset { get; }

    public PageRequest(int limit, int offset)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, MinLimit);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, MaxLimit);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        Limit = limit;
        Offset = offset;
    }
}
