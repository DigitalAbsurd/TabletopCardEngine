# TabletopCardEngine.AspNetCore

ASP.NET Core integration for `TabletopCardEngine`. Wires the engine into a host and exposes it over HTTP with health checks and a REST API.

```bash
dotnet add package TabletopCardEngine.AspNetCore
```

Register the engine with a database provider, add a health check, and map the API:

```csharp
using TabletopCardEngine;
using TabletopCardEngine.AspNetCore;
using TabletopCardEngine.Sqlite;

var builder = WebApplicationBuilder.CreateBuilder(args);
builder.Services.AddTabletopCardEngine(tce => tce.AddHoldem(
    builder.Configuration.GetSection("TabletopCardEngine:Holdem"),
    db => db.UseHoldemSqlite(builder.Configuration.GetConnectionString("TabletopCardEngine")!)
));
builder.Services.AddHealthChecks().AddHoldemCheck();

var app = builder.Build();
app.MapHealthChecks("/health");
app.MapHoldemApi();
await app.RunAsync();
```

The health check calls the engine to report the database schema state. The API exposes endpoints to create games, start hands, play actions, and retrieve game state.

**Documentation:** [ASP.NET Core](https://tabletopcardengine.readthedocs.io/en/stable/aspnetcore/) · [Repository](https://github.com/DigitalAbsurd/TabletopCardEngine)
