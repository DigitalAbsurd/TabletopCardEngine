# Tournaments

A tournament is a freezeout: everybody pays the same buy-in, starts with the same stack, and plays until one player has all the chips. Blinds (and optionally antes) rise on a clock, tables are broken and balanced as players bust, and the places share a prize pool. One entry point, `IHoldemTournament.ApplyAsync`, runs it: a sit-and-go is a tournament that fits one table, a multi-table tournament is one that does not.

## An orchestrator of ordinary games

The engine does not play a tournament hand itself. Each table of a tournament is an ordinary Hold'em game, the same `Game` a cash game is, and the players act on it through `IHoldemTable` with the table's game id: `Play` and `GetGame`. The tournament creates those games, seats the players, sets the blinds and the ante each hand is dealt at, records who busted and pays the places. It keeps its own state in three tables of the same database (`Tournament`, `TournamentEntry`, `TournamentTable`), saved in the same transaction as the game it touches. [ADR-0011](../adr/0011-tournaments-orchestrate-holdem-games.md) records the decisions and the reasons for them.

Like `IHoldemTable`, `IHoldemTournament` is registered by `AddHoldem`, holds no state in memory, and reloads the tournament from the database on every call:

```csharp
Task<HoldemResult<TournamentData>> IHoldemTournament.ApplyAsync(TournamentAction action, CancellationToken ct)
```

`TournamentData` is the whole tournament as it is after the call: the options, the state, the entries with their seats, places and prizes, the tables with their game ids, the current level and when it ends, and a `Version`.

## The lifecycle

```text
CreateTournament → Register* → StartTournament → (play on the tables, DealTable between hands)* → finished
```

| Action | Does |
|---|---|
| `CreateTournament(options, tournamentId?)` | Creates the tournament `Registering`, at version 1. A repeated id fails with `TournamentAlreadyExists`. |
| `GetTournament(id)` | Reads it back; nothing is saved and the version stays. |
| `Register(id, name)` | Adds a player, while registering. The name is 1 to 64 characters and unique regardless of case, else `InvalidEntry`; past `MaxEntries` it is `TournamentFull`. The entry gets an `EntryNo` one above the highest ever given, so a number is never reused. |
| `Unregister(id, entryNo)` | Withdraws an entry, while registering; its buy-in leaves the prize pool. |
| `StartTournament(id, seatDrawSeed?)` | Closes registration, draws the seats, opens the tables and deals their first hands; the clock starts. Fewer than `MinEntries` entries fails with `NotEnoughPlayers`. |
| `DealTable(id, tableNo)` | The host's call between two hands of one table: records the busts, breaks or balances tables, and deals the next hand, or finishes the tournament. |

`Register`, `Unregister` and `StartTournament` after the start fail with `TournamentAlreadyStarted`. `DealTable` before the start fails with `TournamentNotStarted` and after the end with `TournamentFinished`. A tournament that does not exist fails every action but `CreateTournament` with `TournamentNotFound`. Registration is closed for good once the tournament starts: there are no rebuys, add-ons or late entries.

## The options

`TournamentOptionData` is built with an object initializer, and its rules never change afterwards. Every field has a default except the levels, which you must set. An option out of range fails `CreateTournament` with `InvalidTournamentOptions`, and a betting structure the engine does not play with `UnsupportedGameOptions`.

| Field | Default | Meaning |
|---|---|---|
| `BettingStructure` | `NoLimit` | How bets are sized at every table: No-Limit, Pot-Limit or Fixed-Limit, as for [a cash game](betting-structures.md). |
| `TableFormat` | `SixMax` | The format of every table, which caps `MaxPlayersPerTable`: `HeadsUp` exactly 2, `SixMax` 2 to 6, `FullRing` 7 to 10. |
| `MaxPlayersPerTable` | `6` | The seats of every table, inside the range of the format. |
| `RaiseCap` | `4` | The most bets a street holds under Fixed-Limit (`null` for no cap); ignored by the other structures. |
| `AnteType` | `Classic` | Who posts the ante of a level that has one: every seat dealt in (`Classic`), or the big blind alone (`BigBlind`). See [Buy-ins, blinds and antes](buy-ins-and-blinds.md). |
| `StartingStack` | `10000` | The chips every player starts with: positive, at most two decimal places. |
| `BuyIn` | `0` | What each entry adds to the prize pool; `0` is a free tournament. A fee on top of it is the host's business. |
| `MinEntries` | `2` | The fewest entries the tournament starts with; at least 2. |
| `MaxEntries` | `null` | The most entries registration takes (`null` for no limit); at least `MinEntries` when set. |
| `Levels` | empty | 1 to 100 `TournamentLevelData`, played in order, whose small blinds never decrease. They must fit in 4 000 characters of JSON. |
| `Payouts` | `[100]` | The percentage of the prize pool each place wins, first place first: 1 to `MinEntries` values, each positive and no larger than the one before, adding up to exactly 100. |

A `TournamentLevelData` is a `SmallBlind` (the big blind is twice it), an `Ante` (`0` for none, posted as `AnteType` says, dead money as in [cash games](buy-ins-and-blinds.md)) and a `Duration` of at least one minute and a whole number of seconds. The last level lasts until the tournament ends, whatever its duration.

## The level clock

The level is a function of the time since `StartTournament`, and of nothing else: the first level whose end, counted from the start, is still ahead, or the last one once every end has passed. The engine reads the time from a `TimeProvider`, `TimeProvider.System` by default. A test or a replay passes a clock it controls:

```csharp
services.AddTabletopCardEngine(tce => tce.AddHoldem(
    db => db.UseHoldemSqlite(connectionString),
    options => options.WithTimeProvider(myClock)));
```

A hand keeps the level it was dealt at: the clock is read when `DealTable` deals a hand, so a level change takes effect at the next deal and never in the middle of a hand. Every table reads the same clock. `TournamentData.LevelIndex` (an index into `Levels`, `0` for the first, like `HandData.BlindLevel`) and `LevelEndsAt` show the level the clock gives at the moment of the call; `LevelEndsAt` is `null` before the start and on the last level. The hand's own blinds and ante are `HandData.SmallBlind`, `BigBlind` and `Ante`.

## Seating, breaking and balancing

`StartTournament` splits the entries into as few tables of `MaxPlayersPerTable` seats as they fit in, whose sizes differ by at most one, and draws the seats and the buttons at random. `seatDrawSeed` makes the draw reproducible, and the seed used is kept either way. Every table is a game listed in `TournamentData.Tables` with its `TableNo` and `GameId`, whose first hand is dealt at once at the first level.

After that, the tables are kept in order at `DealTable`, one table at a time:

- when the players left fit into one table fewer, the table being dealt breaks: its players move to the tables with the fewest players, its game is finished and it is marked `IsBroken`;
- otherwise, while the table has two or more players than the shortest one, it sends the player due to post the next big blind to the shortest table;
- when it is that much larger but none of its seated players can be given away, as when most of it are players moving in from a broken table, it sends on a player that is moving to it instead.

Only the table being dealt breaks or gives a player away, and a move does not touch the target's game, so the other tables play on undisturbed. A moved player is an entry in the state `Moving`, carrying its stack with a seat reserved at the target, until the target's next `DealTable` seats it, with that stack, in the hand it deals. Every entry shows its `State` (`Registered`, `Seated`, `Moving`, `Eliminated`, `Winner`), `TableNo` and `SeatNo`.

## Places, ties and prizes

`DealTable` places the players who ended their table's last hand without chips. With `P` players left before the call and `b` of them out now, they take the places `P - b + 1` to `P`, the bigger stack at the start of that hand taking the better place. Players who started that hand with the same stack tie: each gets the best place of the group and they share the prizes of the places the group covers equally, to the cent, any cent left over going to the lower `EntryNo`. Across tables, the order is the order in which `DealTable` recorded the busts.

The prize pool is `BuyIn` times the number of entries (`TournamentData.PrizePool`). The prize of place `k` is the pool times the `k`-th of `Payouts`, divided by 100 and rounded to the cent, and `0` past the last payout; first place gets what the others leave, so the prizes add up to the pool exactly. When one player is left, that player takes first place, the tournament becomes `Finished`, `FinishedAt` is set and every table's game is finished.

## What the tables refuse

A tournament table is dealt, seated and ended by its tournament. `IHoldemTable` answers every action on it but `Play` and `GetGame` with `ManagedByTournament`: `StartGame`, `StartHand`, `FinishGame`, `LeaveSeat`, `KickSeat` and `TopUp` among them. The guard runs after the game is found and before the `ExpectedVersion` check. Once the tournament has finished, `Play` fails with `GameFinished`, as on any finished game.

## Concurrency

`TournamentData.Version` is the tournament's optimistic concurrency token, with the rules of [`GameData.Version`](index.md#optimistic-concurrency-with-expectedversion): it starts at `1`, every call that saves increments it, `GetTournament` does not, and `TournamentAction.ExpectedVersion` that no longer matches fails with `Conflict` before anything runs. `Play` saves the table's game only and never the tournament, so players at different tables never wait for each other.

`DealTable` saves the tournament and one table's game together. When two calls race on the same tournament, even for different tables, the loser gets `Conflict` and nothing is saved: reload with `GetTournament` and call again. The engine never retries and takes no lock.

## A host loop for a sit-and-go

The engine deals nothing by itself: a host that never calls `DealTable` stalls its tournament. This sit-and-go registers three players, starts, and drives the table until one is left. (Here every player shoves when raising is open, so that the example ends quickly; a real host asks its players.) A test runs it as it is written, in `TournamentDocsExampleTests`.

```csharp
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Tournaments;
using TabletopCardEngine.InMemory;

var services = new ServiceCollection();
services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemInMemory("holdem")));
using var provider = services.BuildServiceProvider();

// Create or upgrade the schema once, at start-up.
await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
var table = provider.GetRequiredService<IHoldemTable>();
var tournaments = provider.GetRequiredService<IHoldemTournament>();

// A sit-and-go: three players with 1000 each, a buy-in of 10, the top two places paid 65/35,
// and three blind levels of ten minutes, the second and third with an ante.
var created = await tournaments.ApplyAsync(TournamentAction.CreateTournament(new TournamentOptionData
{
    StartingStack = 1000,
    BuyIn = 10,
    Payouts = [65, 35],
    Levels =
    [
        new TournamentLevelData { SmallBlind = 25, Duration = TimeSpan.FromMinutes(10) },
        new TournamentLevelData { SmallBlind = 50, Ante = 5, Duration = TimeSpan.FromMinutes(10) },
        new TournamentLevelData { SmallBlind = 100, Ante = 10, Duration = TimeSpan.FromMinutes(10) },
    ],
}));
var tournamentId = created.Value!.TournamentId;

foreach (var name in new[] { "Alice", "Bob", "Carol" })
{
    await tournaments.ApplyAsync(TournamentAction.Register(tournamentId, name));
}

// StartTournament closes registration, draws the seats and deals the first hand of every table.
var data = (await tournaments.ApplyAsync(TournamentAction.StartTournament(tournamentId))).Value!;

while (data.State == TournamentState.Running)
{
    foreach (var tournamentTable in data.Tables.Where(t => !t.IsBroken))
    {
        // Play the hand on the table's game, as for a cash game: shove when raising is open, otherwise
        // check or call.
        var game = (await table.ApplyAsync(HoldemAction.GetGame(tournamentTable.GameId))).Value!;
        while (game.HandData!.NextPlayerSeatNo != 0)
        {
            var offered = game.HandData.Evaluations.Select(e => e.PlayerActionType).ToList();
            var action = offered.Contains(PlayerActionType.Raise) ? PlayerActionType.AllIn
                : offered.Contains(PlayerActionType.Check) ? PlayerActionType.Check
                : PlayerActionType.Call;
            var played = await table.ApplyAsync(HoldemAction.Play(tournamentTable.GameId, action));
            if (!played.IsSuccess)
            {
                throw new InvalidOperationException(played.Errors[0].Message);
            }

            game = played.Value!;
        }

        // Nothing deals the next hand by itself: DealTable records the busts and deals it, or ends the tournament.
        var dealt = await tournaments.ApplyAsync(TournamentAction.DealTable(tournamentId, tournamentTable.TableNo));
        if (!dealt.IsSuccess)
        {
            throw new InvalidOperationException(dealt.Errors[0].Message);
        }

        data = dealt.Value!;
    }
}

foreach (var entry in data.Entries.OrderBy(e => e.Place))
{
    Console.WriteLine($"{entry.Place}. {entry.Name}: {entry.Prize}");
}
```

A multi-table host runs the same two steps for every table, but re-reads `Tables` after each `DealTable`, because a call can break a table (a later `DealTable` on it fails with `InvalidTable`) or seat players at another. A table left with fewer than two players deals nothing and waits for arrivals; the call still succeeds. The same routes are available over HTTP, see [Tournaments in the ASP.NET Core guide](../aspnetcore.md#tournaments).

## What is out of scope

Rebuys, add-ons, late registration, bounties, breaks, hand-for-hand play, deal-making, disqualification and satellites are not part of 1.4 (ADR-0011, point 9). Who may call each tournament action is the host's business, as it is for `KickSeat`.
