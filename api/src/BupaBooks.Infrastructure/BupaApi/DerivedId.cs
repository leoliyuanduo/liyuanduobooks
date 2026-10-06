using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BupaBooks.Infrastructure.BupaApi;

/// <summary>
/// Fallback identifiers for records that arrive without one (the test endpoint
/// sends none). Deterministic: the same payload always yields the same ids, so
/// caching, ETags and paging stay stable. They are only stable while upstream
/// data is unchanged, which is why real upstream ids always take precedence.
/// </summary>
internal static class DerivedId
{
    private const char Separator = '\u001f';

    public static string ForOwner(int index, string name, int age) =>
        Hash("owner", index.ToString(CultureInfo.InvariantCulture), name, age.ToString(CultureInfo.InvariantCulture));

    public static string ForBook(string ownerId, int index, string title, string type) =>
        Hash("book", ownerId, index.ToString(CultureInfo.InvariantCulture), title, type);

    private static string Hash(params string[] parts)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(Separator, parts)));
        return "d_" + Convert.ToHexStringLower(bytes)[..20];
    }
}
