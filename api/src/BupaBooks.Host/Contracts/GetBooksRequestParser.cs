using System.Globalization;
using BupaBooks.Application.Books.GetBooks;
using BupaBooks.Application.Domain;
using BupaBooks.Application.Paging;

namespace BupaBooks.Host.Contracts;

/// <summary>
/// Turns raw query-string values into a validated use-case query, collecting every
/// error at once (RFC 9457 validation problem). Parsing by hand keeps the rules
/// explicit: only the documented lowercase-insensitive names are accepted, never
/// enum numbers like "1".
/// </summary>
internal static class GetBooksRequestParser
{
    private static readonly Dictionary<string, AgeCategory> AgeCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["adult"] = AgeCategory.Adult,
        ["child"] = AgeCategory.Child,
    };

    private static readonly Dictionary<string, BookFormat> Formats = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hardcover"] = BookFormat.Hardcover,
        ["paperback"] = BookFormat.Paperback,
        ["ebook"] = BookFormat.Ebook,
    };

    public static bool TryParse(
        string? ageCategory,
        string? format,
        string? limit,
        string? cursor,
        out GetBooksQuery? query,
        out Dictionary<string, string[]> errors)
    {
        errors = [];
        query = null;

        AgeCategory parsedCategory = default;
        if (string.IsNullOrWhiteSpace(ageCategory))
        {
            errors["ageCategory"] = ["ageCategory is required. Allowed values: adult, child."];
        }
        else if (!AgeCategories.TryGetValue(ageCategory.Trim(), out parsedCategory))
        {
            errors["ageCategory"] = ["Allowed values: adult, child."];
        }

        BookFormat? parsedFormat = null;
        if (!string.IsNullOrWhiteSpace(format))
        {
            if (Formats.TryGetValue(format.Trim(), out var value))
            {
                parsedFormat = value;
            }
            else
            {
                errors["format"] = ["Allowed values: hardcover, paperback, ebook."];
            }
        }

        var parsedLimit = PageRequest.DefaultLimit;
        if (!string.IsNullOrWhiteSpace(limit)
            && (!int.TryParse(limit, NumberStyles.None, CultureInfo.InvariantCulture, out parsedLimit)
                || parsedLimit < PageRequest.MinLimit
                || parsedLimit > PageRequest.MaxLimit))
        {
            errors["limit"] = [$"limit must be a whole number from {PageRequest.MinLimit} to {PageRequest.MaxLimit}."];
        }

        var offset = 0;
        if (!string.IsNullOrWhiteSpace(cursor) && !Cursor.TryDecode(cursor, out offset))
        {
            errors["cursor"] = ["cursor is invalid. Use the nextCursor value from a previous response."];
        }

        if (errors.Count > 0)
        {
            return false;
        }

        query = new GetBooksQuery(new BookCriteria(parsedCategory, parsedFormat), new PageRequest(parsedLimit, offset));
        return true;
    }
}
