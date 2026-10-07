using BupaBooks.Application.Books.GetBooks;
using BupaBooks.Host.Contracts;
using BupaBooks.Host.Http;
using BupaBooks.Host.RateLimiting;
using BupaBooks.Host.ApiDocs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace BupaBooks.Host.Endpoints;

/// <summary>
/// GET /api/v1/books: books owned by one age category, optionally one format,
/// alphabetical, paged. The client states intent; every business rule runs here.
/// </summary>
public static class BooksEndpoints
{
    public static IEndpointRouteBuilder MapBooksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/books")
            .WithTags("Books")
            .RequireRateLimiting(RateLimitingSetup.PerClientIpPolicy);

        group.MapGet("", GetBooksAsync)
            .WithName(GetBooksOperationTransformer.OperationId)
            .WithSummary("List books owned by an age category")
            .WithDescription(
                "Returns books owned by adults (18+) or children (17 and under), optionally filtered by format, " +
                "sorted alphabetically by title (case-insensitive, then by id). Supports cursor paging and ETags.")
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);

        return app;
    }

    private static async Task<Results<Ok<BooksPageResponse>, ValidationProblem, StatusCodeHttpResult>> GetBooksAsync(
        // Bound from the query string. Kept as strings so validation is explicit and
        // every error is reported at once (see GetBooksRequestParser).
        string? ageCategory,
        string? format,
        string? limit,
        string? cursor,
        GetBooksHandler handler,
        IOptions<HttpJsonOptions> jsonOptions,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!GetBooksRequestParser.TryParse(ageCategory, format, limit, cursor, out var query, out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var page = await handler.HandleAsync(query!, cancellationToken);
        var body = page.ToResponse();

        var etag = HttpCaching.ComputeETag(body, jsonOptions.Value.SerializerOptions);
        httpContext.Response.Headers.ETag = etag;
        httpContext.Response.Headers.CacheControl = HttpCaching.CacheControlValue;

        if (HttpCaching.MatchesIfNoneMatch(httpContext.Request, etag))
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        return TypedResults.Ok(body);
    }
}
