using System.Text.Json;
using System.Text.Json.Serialization;
using BupaBooks.Application.Abstractions;
using BupaBooks.Infrastructure.BupaApi.Dtos;
using Microsoft.Extensions.Logging;

namespace BupaBooks.Infrastructure.BupaApi;

/// <summary>
/// Validates the untrusted upstream payload record by record.
/// Policy (ADR-0003): a payload that is not an array breaks the contract and fails;
/// individual bad records are skipped and logged so one bad row cannot blank the page.
/// Output DTOs are guaranteed valid, so the mapper never has to defend itself.
/// </summary>
internal static class BupaPayloadReader
{
    // Web defaults (camelCase, case-insensitive) but STRICT numbers: the Web defaults also set
    // AllowReadingFromString, which silently accepted "age": "30". A type change upstream is
    // contract drift we want to see (and reject), not paper over.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.Strict,
    };

    public static BupaBookOwnerDto[] Read(JsonElement root, ILogger logger)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new BookSourceException(
                BookSourceFailure.InvalidResponse,
                $"Expected a JSON array of book owners but received {root.ValueKind}.");
        }

        var owners = new List<BupaBookOwnerDto>();
        var index = 0;
        foreach (var element in root.EnumerateArray())
        {
            var owner = TryReadOwner(element, index, logger);
            if (owner is not null)
            {
                owners.Add(owner);
            }

            index++;
        }

        return owners.ToArray();
    }

    private static BupaBookOwnerDto? TryReadOwner(JsonElement element, int index, ILogger logger)
    {
        BupaBookOwnerDto? dto;
        try
        {
            dto = element.Deserialize<BupaBookOwnerDto>(JsonOptions);
        }
        catch (JsonException ex)
        {
            // Never log the raw record: it contains personal data (name, age).
            Log.OwnerRecordSkipped(logger, index, $"malformed JSON at {ex.Path ?? "root"}");
            return null;
        }

        if (dto is null)
        {
            Log.OwnerRecordSkipped(logger, index, "record is null");
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            Log.OwnerRecordSkipped(logger, index, "name is missing");
            return null;
        }

        if (dto.Age is not { } age || age < 0)
        {
            Log.OwnerRecordSkipped(logger, index, "age is missing or negative");
            return null;
        }

        var validBooks = new List<BupaBookDto>();
        var books = dto.Books ?? [];
        for (var bookIndex = 0; bookIndex < books.Length; bookIndex++)
        {
            var book = books[bookIndex];
            if (book is null || string.IsNullOrWhiteSpace(book.Name))
            {
                Log.BookRecordSkipped(logger, index, bookIndex, "title is missing");
                continue;
            }

            validBooks.Add(book with { Name = book.Name.Trim(), Type = book.Type?.Trim() ?? string.Empty });
        }

        return dto with { Name = dto.Name.Trim(), Books = validBooks.ToArray() };
    }
}
