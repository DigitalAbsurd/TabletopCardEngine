using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using TabletopCardEngine.AspNetCore;
using TabletopCardEngine.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHoldemEngine(
    builder.Configuration.GetSection("TabletopCardEngine"),
    db => db.UseHoldemSqlite(builder.Configuration.GetConnectionString("TabletopCardEngine")!));
builder.Services.AddHealthChecks().AddHoldemEngineCheck();

// Traces and metrics need the TabletopCardEngine.OpenTelemetry package, which this sample does not
// reference (a sample has no PackageReference). In your own host, add it like this:
//
// builder.Services.AddOpenTelemetry()
//     .AddTabletopCardEngineInstrumentation()
//     .WithTracing(tracing => tracing.AddConsoleExporter())
//     .WithMetrics(metrics => metrics.AddConsoleExporter());

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteHealthJsonAsync });
app.MapHoldemEngineApi();

app.Run();

static Task WriteHealthJsonAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var entries = report.Entries.ToDictionary(
        entry => entry.Key,
        entry => new
        {
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            data = entry.Value.Data,
        });
    return context.Response.WriteAsync(JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        entries,
    }));
}
