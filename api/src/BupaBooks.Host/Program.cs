using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using BupaBooks.Application;
using BupaBooks.Host.Endpoints;
using BupaBooks.Host.ErrorHandling;
using BupaBooks.Host.Http;
using BupaBooks.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Composition root: the only place that knows every layer. ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Enums travel as camelCase strings ("hardcover"), which is also what the OpenAPI schema advertises.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

// RFC 9457 problem details for every error, each carrying a traceId to find it in the logs.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<BookSourceExceptionHandler>();

builder.Services.AddOpenApi();
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
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

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
