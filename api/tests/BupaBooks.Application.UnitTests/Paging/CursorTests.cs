using BupaBooks.Application.Paging;

namespace BupaBooks.Application.UnitTests.Paging;

public class CursorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(1_000_000)]
    public void Encoded_cursors_round_trip(int offset)
    {
        Cursor.TryDecode(Cursor.Encode(offset), out var decoded).ShouldBeTrue();
        decoded.ShouldBe(offset);
    }

    [Fact]
    public void Cursors_are_url_safe() =>
        Cursor.Encode(123_456).ShouldAllBe(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-cursor!")]
    [InlineData("bzotMQ")] // "o:-1": negative offsets are rejected
    [InlineData("eDox")] // "x:1": wrong prefix
    public void Invalid_cursors_are_rejected(string? cursor) =>
        Cursor.TryDecode(cursor, out _).ShouldBeFalse();
}
