using BupaBooks.Application.Paging;

namespace BupaBooks.Application.UnitTests.Paging;

public class PageTests
{
    private static readonly int[] Numbers = [1, 2, 3, 4, 5];

    [Fact]
    public void First_page_has_a_next_cursor()
    {
        var page = Page.Create(Numbers, new PageRequest(limit: 2, offset: 0), n => n);

        page.Items.ShouldBe([1, 2]);
        page.TotalCount.ShouldBe(5);
        page.NextCursor.ShouldNotBeNull();
        Cursor.TryDecode(page.NextCursor, out var next).ShouldBeTrue();
        next.ShouldBe(2);
    }

    [Fact]
    public void Last_page_has_no_next_cursor()
    {
        var page = Page.Create(Numbers, new PageRequest(limit: 2, offset: 4), n => n);

        page.Items.ShouldBe([5]);
        page.NextCursor.ShouldBeNull();
    }

    [Fact]
    public void Offset_past_the_end_returns_an_empty_page()
    {
        var page = Page.Create(Numbers, new PageRequest(limit: 2, offset: 10), n => n);

        page.Items.ShouldBeEmpty();
        page.NextCursor.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(101, 0)]
    [InlineData(10, -1)]
    public void Invalid_page_requests_are_rejected(int limit, int offset) =>
        Should.Throw<ArgumentOutOfRangeException>(() => new PageRequest(limit, offset));
}
