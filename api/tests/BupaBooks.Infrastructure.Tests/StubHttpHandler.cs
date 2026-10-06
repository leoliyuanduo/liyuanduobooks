using System.Net;
using System.Text;

namespace BupaBooks.Infrastructure.Tests;

/// <summary>Replaces the network: returns canned responses (or throws) and counts calls.</summary>
internal sealed class StubHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    private int _calls;

    public int Calls => Volatile.Read(ref _calls);

    public static StubHttpHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new((_, _) => Task.FromResult(JsonResponse(json, status)));

    public static StubHttpHandler Throws(Exception exception) =>
        new((_, _) => Task.FromException<HttpResponseMessage>(exception));

    public static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        return respond(request, cancellationToken);
    }
}
