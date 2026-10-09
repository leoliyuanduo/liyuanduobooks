using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BupaBooks.Infrastructure.BupaApi;

/// <summary>
/// Identifiers for upstream records, which carry none. Each owned copy needs a stable
/// id (React list keys, the sort tie-break, deterministic paging), so the server derives
/// one from the record's content and position. Deterministic: the same payload always
/// yields the same ids, so caching, ETags and paging stay stable. Ids stay stable while
/// the upstream data is unchanged; clients must treat them as opaque.
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
