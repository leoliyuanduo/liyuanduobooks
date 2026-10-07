namespace BupaBooks.Infrastructure.BupaApi;

/// <summary>
/// The Bupa test API intermittently answers <c>200 OK</c> with an empty body
/// (observed roughly 1 call in 6, 7 Oct 2026). A retry policy that only looks at
/// status codes treats that as success. This handler sits INSIDE the resilience
/// pipeline (closest to the network) and turns an empty success into a transient
/// <see cref="HttpRequestException"/>, which the standard pipeline retries.
/// </summary>
internal sealed class EmptyResponseGuardHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return response;
        }

        // Buffering is fine: the payload is a few hundred bytes, and the client reads it once afterwards.
        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (body.AsSpan().Trim(" \t\r\n"u8).IsEmpty)
        {
            var status = response.StatusCode;
            response.Dispose();
            throw new HttpRequestException(
                "Upstream returned a success status with an empty body.",
                inner: null,
                statusCode: status);
        }

        return response;
    }
}
