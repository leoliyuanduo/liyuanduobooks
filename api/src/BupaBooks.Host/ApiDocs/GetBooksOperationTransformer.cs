using System.Globalization;
using System.Text.Json.Nodes;
using BupaBooks.Application.Paging;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BupaBooks.Host.ApiDocs;

/// <summary>
/// The endpoint binds query values as strings so it can validate them explicitly
/// (see GetBooksRequestParser). Left alone, the generated OpenAPI would therefore
/// advertise "any string". This transformer publishes the real contract (allowed
/// values, required, numeric bounds), which flows into the client's generated
/// TypeScript types: <c>ageCategory: "adult" | "child"</c> instead of <c>string</c>.
/// </summary>
internal sealed class GetBooksOperationTransformer : IOpenApiOperationTransformer
{
    public const string OperationId = "GetBooks";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (operation.OperationId != OperationId || operation.Parameters is null)
        {
            return Task.CompletedTask;
        }

        foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
        {
            switch (parameter.Name)
            {
                case "ageCategory":
                    parameter.Required = true;
                    parameter.Description = "Age category of the owners: adult (18 and over) or child (17 and under).";
                    parameter.Schema = StringEnum("adult", "child");
                    break;
                case "format":
                    parameter.Description = "Only return books in this format. Omit for all formats.";
                    parameter.Schema = StringEnum("hardcover", "paperback", "ebook");
                    break;
                case "limit":
                    parameter.Description = $"Page size. Default {PageRequest.DefaultLimit}.";
                    parameter.Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer,
                        Format = "int32",
                        Minimum = PageRequest.MinLimit.ToString(CultureInfo.InvariantCulture),
                        Maximum = PageRequest.MaxLimit.ToString(CultureInfo.InvariantCulture),
                        Default = JsonValue.Create(PageRequest.DefaultLimit),
                    };
                    break;
                case "cursor":
                    parameter.Description = "Opaque cursor from page.nextCursor of the previous response.";
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private static OpenApiSchema StringEnum(params string[] values) =>
        new()
        {
            Type = JsonSchemaType.String,
            Enum = values.Select(value => (JsonNode)JsonValue.Create(value)!).ToList(),
        };
}
