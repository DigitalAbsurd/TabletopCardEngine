using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using TabletopCardEngine;
using TabletopCardEngine.AspNetCore;
using TabletopCardEngine.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTabletopCardEngine(tce => tce.AddHoldem(
    builder.Configuration.GetSection("TabletopCardEngine:Holdem"),
    HoldemServerDatabase.Configure(builder.Configuration)));
builder.Services.AddHealthChecks().AddHoldemCheck();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteHealthJsonAsync });
app.MapHoldemApi();
app.MapOpenApi();

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
