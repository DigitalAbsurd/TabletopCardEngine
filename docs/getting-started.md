# Getting started

## Prerequisites

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

## Create a console app and install the package

```bash
dotnet new console -n HoldemQuickStart
cd HoldemQuickStart
dotnet add package TabletopCardEngine.InMemory --prerelease
```

The InMemory package brings the core package with it.

## Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.InMemory;

var services = new ServiceCollection();
services.AddHoldemEngine(db => db.UseHoldemInMemory("holdem"));
using var provider = services.BuildServiceProvider();

// Create or upgrade the schema once, at start-up.
await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
var engine = provider.GetRequiredService<IHoldemTable>();

// A heads-up game: blinds 1/2, two players with 100 each.
var created = await engine.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
{
    MaxPlayersCount = 2,
    InitialSmallBlind = 1,
}));
var gameId = created.Value.GameId;

// StartGame seats the players, deals hand 1 and posts the blinds.
var game = (await engine.ApplyAsync(HoldemAction.StartGame(gameId,
[
    new PlayerJoinData("Alice", 1, 100),
    new PlayerJoinData("Bob", 2, 100),
]))).Value;

// Play the hand to the end: check when allowed, otherwise call.
while (game.HandData!.NextPlayerSeatNo != 0)
{
    var canCheck = game.HandData.Evaluations.Any(e => e.PlayerActionType == PlayerActionType.Check);
    var action = canCheck ? PlayerActionType.Check : PlayerActionType.Call;
    var result = await engine.ApplyAsync(HoldemAction.Play(gameId, action));
    if (!result.IsSuccess)
    {
        Console.WriteLine(result.Errors[0].Message);
        return 1;
    }

    game = result.Value;
}

Console.WriteLine($"Board: {string.Join(" ", game.HandData.CommunityCards)}");
foreach (var seat in game.HandData.Seats)
{
    // Stack excludes the winnings until the next hand; the outcome is HandResult.Net.
    var net = seat.HandResult!.Net;
    Console.WriteLine($"{seat.Name}: {string.Join(" ", seat.Hole ?? [])}, {seat.HandResult.Description}, {net:+0;-0;0}");
}

return 0;
```

Run it with:

```bash
dotnet run
```

Sample output (the cards differ on every run):

```
Board: 6d 8d 2h 8s 8c
Alice: 5h 4d, Three of a kind, -2
Bob: 2d ah, Full house, +2
```

## What just happened

**Registration and the provider package.** The service collection creates the dependency injection container, `AddHoldemEngine` wires up the core library, and `UseHoldemInMemory` keeps the database in the process's memory, under the store name `holdem`. The InMemory package brings the core with it and has no native dependencies, so only one `dotnet add package` is needed and it runs on any platform .NET runs on. It suits trying the engine out, tests and demos; for data that must outlive the process, switch to the SQLite, PostgreSQL or SQL Server package (see [Database providers](providers.md)).

**The schema.** `IHoldemSchemaManager.MigrateAsync()` creates or upgrades the database schema once, at start-up. On InMemory it simply creates the store; on a relational provider it applies the package's migrations. The in-memory store lives only as long as the process, so every run starts from an empty database.

**Creating a game.** `HoldemAction.CreateGame` creates a new game with the blinds and player count. In this example, a heads-up game with 1/2 blinds. The `GameData` returned holds the game ID and other details.

**Starting the game.** `HoldemAction.StartGame` seats the players, deals the first hand, and posts the blinds. Each player joins with a name, seat number, and buy-in amount. After this call, `game.HandData` holds the current hand's state: the board (initially empty), each player's hole cards, and `Evaluations` — the list of legal actions for the player whose turn it is.

**Playing the hand.** The loop checks whether the player on the clock can check; if not, it calls. `NextPlayerSeatNo == 0` ends the loop — when there is no more acting to do, the hand is over. Each `Play` action advances the hand, dealing new community cards when the street changes, and updating `Evaluations` for the next player to act.

**Hand outcome.** When the hand is over, `NextPlayerSeatNo == 0` exits the loop. `HandResult.Net` holds the outcome: positive for a winner, negative for a loser, zero for a fold or uncalled bet. `Stack` still shows the start stack minus what was wagered this hand; the winnings reach the stack when the next hand starts with `StartHand`.

**The next hand.** To play another hand, call `HoldemAction.StartHand(gameId)` without changing the players or the stacks. The blinds post again, and play resumes.

## Next steps

- **[Key concepts](concepts/index.md)** — blinds, seats, streets, board, hand evaluation, and ledger
- **[Database providers](providers.md)** — SQLite, PostgreSQL, SQL Server and in-memory storage
- **[ASP.NET Core](aspnetcore.md)** — build a REST API with the TabletopCardEngine.AspNetCore package
- **[Container images](docker.md)** — run the API in Docker
