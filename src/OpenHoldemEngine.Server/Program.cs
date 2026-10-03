using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenHoldemEngine;
using OpenHoldemEngine.AspNetCore;
using OpenHoldemEngine.Server;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHoldemEngine(
    builder.Configuration.GetSection("HoldemEngine"),
    HoldemServerDatabase.Configure(builder.Configuration));
builder.Services.AddHealthChecks().AddHoldemEngineCheck();

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
