using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace BupaBooks.Host.RateLimiting;

/// <summary>
/// Inbound rate limiting per client IP. Protects this service (and, through it, the
/// rate-limited upstream) from abuse and runaway clients. Rejections are 429 problem
/// details with Retry-After, which the React client honours.
/// <para>
/// The client IP is only trustworthy if forwarded headers are restricted to known
/// proxies (see Program.cs); otherwise anyone could spoof X-Forwarded-For and get a
/// fresh bucket per request.
/// </para>
/// </summary>
internal static class RateLimitingSetup
{
    public const string PerClientIpPolicy = "per-client-ip";

    public static IServiceCollection AddClientIpRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ClientRateLimitOptions>()
            .Bind(configuration.GetSection(ClientRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;
            options.AddPolicy(PerClientIpPolicy, httpContext =>
            {
                // Read per request (cheap) so configuration and tests can change limits.
                var settings = httpContext.RequestServices.GetRequiredService<IOptions<ClientRateLimitOptions>>().Value;
                return RateLimitPartition.GetTokenBucketLimiter(
                    ClientKey(httpContext),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = settings.PermitLimit,
                        TokensPerPeriod = settings.TokensPerPeriod,
                        ReplenishmentPeriod = settings.ReplenishmentPeriod,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Partition key for a client. IPv6 clients are grouped by /64, because a single
    /// subscriber is usually given a whole /64 and could otherwise rotate addresses
    /// to dodge the limit.
    /// </summary>
    internal static string ClientKey(HttpContext httpContext)
    {
        var address = httpContext.Connection.RemoteIpAddress;
        if (address is null)
        {
            return "unknown";
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            Array.Clear(bytes, 8, 8);
            return new IPAddress(bytes) + "/64";
        }

        return address.ToString();
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests. Please wait and try again.",
            },
        });
    }
}
