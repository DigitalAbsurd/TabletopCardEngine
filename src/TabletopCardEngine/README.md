# TabletopCardEngine

A .NET 10 class library for Texas Hold'em cash games and tournaments. Register the engine with a database provider and drive everything through a single async method: `IHoldemTable.ApplyAsync(action, cancellationToken)`. Tournaments have their own, `IHoldemTournament.ApplyAsync`.

## Not for real-money gambling

TabletopCardEngine is a game-logic library for learning, simulation, research, testing and
play-money games. It is not intended, designed or certified for real-money gambling. It makes no
claim to meet the requirements a gaming regulator sets for such games, such as a certified random
number generator or a tamper-evident audit trail.

Install the core package alongside a provider package (SQLite, PostgreSQL, SQL Server, or in-memory):

```bash
dotnet add package TabletopCardEngine
dotnet add package TabletopCardEngine.Sqlite
```

Here's a minimal example that creates a game, starts a hand, and plays it to the end:

```csharp
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Sqlite;

var services = new ServiceCollection();
services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemSqlite("Data Source=holdem.db")));
using var provider = services.BuildServiceProvider();

await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
var table = provider.GetRequiredService<IHoldemTable>();

var game = await table.ApplyAsync(
    HoldemAction.CreateGame(new GameOptionData { MaxPlayersCount = 2, InitialSmallBlind = 1 })
);
```

A tournament is created, filled and started through `IHoldemTournament`, its tables are ordinary games played through `IHoldemTable`, and the host calls `DealTable` between hands:

```csharp
var tournaments = provider.GetRequiredService<IHoldemTournament>();

var created = await tournaments.ApplyAsync(TournamentAction.CreateTournament(new TournamentOptionData
{
    StartingStack = 1000,
    Levels = [new TournamentLevelData { SmallBlind = 25, Duration = TimeSpan.FromMinutes(10) }],
}));
var tournamentId = created.Value!.TournamentId;
```

The engine supports 2 to 10 seats in three table formats (Heads-Up (2-Max), 6-Max, Full Ring), No-Limit, Pot-Limit and Fixed-Limit betting, optional buy-in limits, top-up and rebuy between hands, kicking a player, blind schedules that raise the blinds by level, optional antes (classic or big blind ante) and freezeout tournaments (sit-and-go and multi-table), and deals the cards, posts blinds, and validates bets automatically. The database holds the ledger—each call rehydrates the game state from rows and applies the action, then saves the result.

**Documentation:** [Getting started](https://tabletopcardengine.readthedocs.io/en/stable/getting-started/) · [Repository](https://github.com/DigitalAbsurd/TabletopCardEngine)

## Questions and feedback

Ask questions and share ideas in [GitHub Discussions](https://github.com/DigitalAbsurd/TabletopCardEngine/discussions), and report bugs in [GitHub Issues](https://github.com/DigitalAbsurd/TabletopCardEngine/issues).
