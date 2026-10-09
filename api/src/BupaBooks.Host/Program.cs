using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using BupaBooks.Application;
using BupaBooks.Host.Endpoints;
using BupaBooks.Host.ErrorHandling;
using BupaBooks.Host.Http;
using BupaBooks.Host.RateLimiting;
using BupaBooks.Host.Security;
using BupaBooks.Host.ApiDocs;
using BupaBooks.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Do not advertise the server technology.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);

// ---- Composition root: the only place that knows every layer. ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Enums travel as camelCase strings ("hardcover"), which is also what the OpenAPI schema advertises.
// Strict number handling: numbers are numbers, so the schema says "integer" rather than "integer | string".
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

// RFC 9457 problem details for every error, each carrying a traceId to find it in the logs.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<BookSourceExceptionHandler>();

// ---- Edge protection: who is calling, how often, and for how long. ----

// Behind a reverse proxy (Front Door, Azure Container Apps ingress, nginx) the client IP arrives in
// X-Forwarded-For. It is only honoured from trusted proxies; otherwise any caller could spoof
// the header and get a fresh rate-limit bucket per request. Configure ONE of:
//   ForwardedHeaders:KnownProxies   fixed proxy IPs
//   ForwardedHeaders:KnownNetworks  proxy subnets (CIDR)
//   ForwardedHeaders:TrustAllProxies  only when the app is unreachable except through the proxy
//                                     (e.g. Azure Container Apps, where ingress IPs are dynamic)
var forwardedHeaders = builder.Configuration.GetSection("ForwardedHeaders");
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1; // only the hop added by our own proxy

    if (forwardedHeaders.GetValue<bool>("TrustAllProxies"))
    {
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        return;
    }

    foreach (var proxy in forwardedHeaders.GetSection("KnownProxies").Get<string[]>() ?? [])
    {
        options.KnownProxies.Add(IPAddress.Parse(proxy));
    }

    foreach (var network in forwardedHeaders.GetSection("KnownNetworks").Get<string[]>() ?? [])
    {
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
    }
});

builder.Services.AddClientIpRateLimiting(builder.Configuration);

// Bound every request's lifetime so slow work cannot pile up and exhaust the server.
// 15 s is above the upstream budget (10 s incl. retries), so upstream errors surface first.
builder.Services.AddRequestTimeouts(options => options.DefaultPolicy = new RequestTimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(15),
    TimeoutStatusCode = StatusCodes.Status504GatewayTimeout,
});

// Brotli/gzip for JSON and the React bundle. Enabled over HTTPS deliberately: responses
// carry no secrets or reflected user input, so compression side channels (BREACH) do not apply.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/problem+json"]);
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Bupa Books BFF",
            Version = "v1",
            Description = "Backend for the Owners and Books app. Applies the business rules (age categories, " +
                          "format filter, alphabetical order) to data from the Bupa coding-test API.",
        };
        return Task.CompletedTask;
    });
    options.AddOperationTransformer<GetBooksOperationTransformer>();
});
builder.Services.AddHealthChecks();

// Traces and metrics for inbound requests and the upstream HttpClient. Exported over OTLP
// (e.g. to Azure Monitor / Application Insights) only when an endpoint is configured.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("bupa-books-bff"))
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation());
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
}

var app = builder.Build();

// ---- HTTP pipeline (order matters). ----
app.UseForwardedHeaders(); // first: everything after sees the real client IP and scheme
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseResponseCompression();
app.UseRateLimiter();
app.UseRequestTimeouts();

app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

// Liveness never depends on the upstream API: an upstream outage must not take this service out of rotation.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.MapBooksEndpoints();

// Unknown /api routes are real 404s (problem details), never the SPA's index.html.
app.MapFallback("/api/{**path}", () => TypedResults.NotFound());

// Serve the React build (copied into wwwroot at publish time) with client-side routing fallback.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

/// <summary>Exposed for WebApplicationFactory in the integration tests.</summary>
public partial class Program;
