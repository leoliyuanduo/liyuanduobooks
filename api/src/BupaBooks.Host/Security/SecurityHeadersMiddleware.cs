namespace BupaBooks.Host.Security;

/// <summary>
/// Baseline browser security headers on every response. The CSP only allows the
/// app's own origin: the React build has no inline scripts and no third-party
/// resources, so nothing legitimate is blocked, and injected scripts cannot run.
/// </summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; " +
        "connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";

        // The interactive API reference (development only) loads its own scripts and styles.
        var isDevApiReference = environment.IsDevelopment() && context.Request.Path.StartsWithSegments("/scalar");
        if (!isDevApiReference)
        {
            headers.ContentSecurityPolicy = ContentSecurityPolicy;
        }

        return next(context);
    }
}
