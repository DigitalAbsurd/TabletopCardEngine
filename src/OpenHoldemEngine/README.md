# OpenHoldemEngine

A .NET 10 class library for Texas Hold'em cash games. Register the engine with a database provider and drive everything through a single async method: `ApplyHoldemActionAsync(action, cancellationToken)`.

Install the core package alongside a provider package (SQLite, PostgreSQL, SQL Server, or in-memory):

```bash
dotnet add package OpenHoldemEngine --prerelease
dotnet add package OpenHoldemEngine.Sqlite --prerelease
```

Here's a minimal example that creates a game, starts a hand, and plays it to the end:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Sqlite;

var services = new ServiceCollection();
services.AddHoldemEngine(db => db.UseHoldemSqlite("Data Source=holdem.db"));
using var provider = services.BuildServiceProvider();

await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
var engine = provider.GetRequiredService<HoldemEngine>();

var game = await engine.ApplyHoldemActionAsync(
    HoldemAction.CreateGame(new GameOptionData { MaxPlayersCount = 2, InitialSmallBlind = 1 })
);
```

The engine supports 2 to 6 seats, no-limit betting, and deals the cards, posts blinds, and validates bets automatically. The database holds the ledger—each call rehydrates the game state from rows and applies the action, then saves the result.

**Documentation:** [Getting started](https://github.com/miekhra/OpenHoldemEngine/blob/main/docs/getting-started.md) · [Repository](https://github.com/miekhra/OpenHoldemEngine)
