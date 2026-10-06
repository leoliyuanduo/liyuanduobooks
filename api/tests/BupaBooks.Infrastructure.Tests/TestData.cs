namespace BupaBooks.Infrastructure.Tests;

internal static class TestData
{
    /// <summary>Verbatim capture of GET /api/v1/bookowners on 6 Oct 2026.</summary>
    public static string LivePayload() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "bookowners.json"));
}
