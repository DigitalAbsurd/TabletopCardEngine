# TabletopCardEngine.AspNetCore

ASP.NET Core integration for `TabletopCardEngine`. Wires the engine into a host and exposes it over HTTP with health checks and a REST API.

```bash
dotnet add package TabletopCardEngine.AspNetCore --prerelease
```

Register the engine with a database provider, add a health check, and map the API:

```csharp
using TabletopCardEngine.AspNetCore;
using TabletopCardEngine.Sqlite;

var builder = WebApplicationBuilder.CreateBuilder(args);
builder.Services.AddHoldemEngine(
    builder.Configuration.GetSection("TabletopCardEngine"),
    db => db.UseHoldemSqlite(builder.Configuration.GetConnectionString("TabletopCardEngine")!)
);
builder.Services.AddHealthChecks().AddHoldemEngineCheck();

var app = builder.Build();
app.MapHealthChecks("/health");
app.MapHoldemEngineApi();
await app.RunAsync();
```

The health check calls the engine to report the database schema state. The API exposes endpoints to create games, start hands, play actions, and retrieve game state.

**Documentation:** [ASP.NET Core](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/aspnetcore.md) · [Repository](https://github.com/DigitalAbsurd/TabletopCardEngine)
