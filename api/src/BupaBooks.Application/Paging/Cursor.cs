using System.Globalization;
using System.Text;

namespace BupaBooks.Application.Paging;

/// <summary>
/// Opaque paging cursor. Today it wraps an offset; because clients must treat it
/// as opaque, it can later become a keyset cursor (last title + id) without a
/// breaking API change.
/// </summary>
public static class Cursor
{
    private const string OffsetPrefix = "o:";

    public static string Encode(int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        var bytes = Encoding.UTF8.GetBytes(OffsetPrefix + offset.ToString(CultureInfo.InvariantCulture));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static bool TryDecode(string? cursor, out int offset)
    {
        offset = 0;
        if (string.IsNullOrWhiteSpace(cursor) || cursor.Length > 64)
        {
            return false;
        }

        var base64 = cursor.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return false;
        }

        var text = Encoding.UTF8.GetString(bytes);
        return text.StartsWith(OffsetPrefix, StringComparison.Ordinal)
            && int.TryParse(text.AsSpan(OffsetPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out offset);
    }
}
