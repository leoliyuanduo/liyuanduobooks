using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BupaBooks.Infrastructure.BupaApi.Dtos;

/// <summary>
/// Reads an id that may arrive as a string ("a1b2") or a number (42). We do not
/// know the upstream id format, so we normalise to an opaque string.
/// </summary>
internal sealed class FlexibleIdConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.HasValueSequence
                ? Encoding.UTF8.GetString(reader.ValueSequence)
                : Encoding.UTF8.GetString(reader.ValueSpan),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unsupported id token: {reader.TokenType}."),
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
