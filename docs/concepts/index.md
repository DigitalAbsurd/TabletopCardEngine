# Key concepts

TabletopCardEngine models Texas Hold'em cash games with a simple command-and-query architecture: every action goes through a single method, the database is the source of truth, and concurrency is managed through optimistic versioning. This page introduces the key ideas you will need to use the engine.

## One method, one result

Everything you do with the engine goes through a single method:

```csharp
Task<HoldemResult<GameData>> IHoldemTable.ApplyAsync(HoldemAction action, CancellationToken ct)
```

### Actions

Actions are built from static factories on `HoldemAction`. The game lifecycle looks like this:

```csharp
var gameId = Guid.NewGuid();

// Create a game
var created = await engine.ApplyAsync(
    HoldemAction.CreateGame(new GameOptionData { TableFormat = HoldemTableFormat.SixMax, MaxPlayersCount = 6 }), ct);
gameId = created.Value.GameId;

// Start it and seat players
var started = await engine.ApplyAsync(
    HoldemAction.StartGame(gameId, new[] {
        new PlayerJoinData { SeatNo = 1, Name = "Alice", BuyInAmount = 100 },
        new PlayerJoinData { SeatNo = 2, Name = "Bob", BuyInAmount = 100 }
    }), ct);

// Deal hands and play actions
var dealt = await engine.ApplyAsync(
    HoldemAction.StartHand(gameId), ct);

var action = await engine.ApplyAsync(
    HoldemAction.Play(gameId, PlayerActionType.Call), ct);

// Read the game state
var current = await engine.ApplyAsync(
    HoldemAction.GetGame(gameId), ct);

// Finish the game between hands
var finished = await engine.ApplyAsync(
    HoldemAction.FinishGame(gameId), ct);
```

The lifecycle is: `CreateGame` → `StartGame` → (`StartHand` | `LeaveSeat` | `Play`)* → `FinishGame`. `GetGame` reads the game back without changing it and is allowed in any state. Once `FinishGame` closes the game, every action except `GetGame` fails with `HoldemErrorCode.GameFinished`.

### Results and errors

`HoldemResult<T>` carries either a value or a list of errors. Check the result and access the value:

```csharp
var result = await engine.ApplyAsync(action, ct);

if (result.IsSuccess)
{
    var game = result.Value;  // GameData
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Code: {error.Code}, Message: {error.Message}");
    }
}
```

Every error has a stable `HoldemErrorCode` that does not change between releases: you can switch on it, persist it, or send it over HTTP. Failed actions save nothing to the database — the call either succeeds in full or changes nothing.

### GameData

`GameData` is what you get back on every call. It holds the game's options, the current hand (or `null` when no hand has been dealt yet), whether the game is finished, and the version number for optimistic concurrency:

```csharp
public record GameData(
    Guid GameId,
    GameOptionData Options,
    HandData? HandData,
    bool IsFinished = false,
    int Version = 0);
```

The hand data includes every seat in the game, the button and blind positions, whose turn it is (`NextPlayerSeatNo`), the board, the pots, and `Evaluations` — the actions the current player may legally take.

## Table formats

The engine supports No-Limit Hold'em in three table formats, chosen when a game is created with `GameOptionData.TableFormat` (`HoldemTableFormat`):

| Format | User-facing name | Seats (`MaxPlayersCount`) | What it is |
|---|---|---|---|
| `HeadsUp` | Heads-Up (2-Max) | exactly 2 | One-on-one play between two players |
| `SixMax` | 6-Max | 2–6 | Short-handed tables of up to six players — the **default** |
| `FullRing` | Full Ring | 7–10 | The traditional full-size table of up to ten players |

When you create a game, if `MaxPlayersCount` falls outside the format's range, the engine returns `HoldemErrorCode.InvalidGameOptions`. If you do not specify a format, `SixMax` is used by default, which ensures existing code that creates games with up to six players continues to work.

**Heads-up changes positions:** In heads-up play with exactly 2 players, the button posts the small blind and acts first pre-flop and last on every street after it. This is different from full-ring and short-handed play, where the button is to the left of the small blind and acts last pre-flop.

## Seats and players

`StartGame` seats at least two players in an empty game and deals the first hand. Between hands, `StartHand` can add new players to empty seats with its optional `JoinSeats` parameter — players join with a new buy-in and begin play on the next hand. When a player runs out of chips (a "bust"), their seat remains occupied until they leave.

`LeaveSeat` stands a player up between hands, freeing the seat for a new player. The departing player's stack leaves with them; it does not stay at the table. A seat can only be vacated when no hand is in progress — `LeaveSeat` returns an error if called during play or when the game is finished.

When fewer than two seats have money left, no more hands can start. The engine returns `HoldemErrorCode.NotEnoughPlayers` and waits for new players to join:

```csharp
// A hand leaves Alice and Bob broke after Bob wins both
var game = /* ... hand finished, each has 0 */;

// Cannot start the next hand
var nextHand = await engine.ApplyAsync(HoldemAction.StartHand(gameId), ct);
// IsSuccess: false, Errors: [{ Code: NotEnoughPlayers, ... }]

// Sit Dana down with a buy-in
game = (await engine.ApplyAsync(
    HoldemAction.StartHand(gameId, [
        new PlayerJoinData("Dana", 3, 100)
    ]), ct)).Value!;

// Now play resumes

// After play, stand Alice up to leave
var afterLeave = (await engine.ApplyAsync(
    HoldemAction.LeaveSeat(gameId, 1), ct)).Value!;
```

The lifecycle remains: `CreateGame` → `StartGame` → (`StartHand` | `LeaveSeat` | player actions)* → `FinishGame`.

## The database is the state

The engine has no in-memory game object. Every call to `ApplyAsync` does this:

1. Loads the game and its current hand from the database
2. Applies the action
3. Saves the changes
4. Projects the aggregate into a `GameData` and returns it

The database is the source of truth, and `GameData` is a snapshot of it at that moment. Consequences for a consumer:

- **Restart safety**: The host can stop and start without losing any game state. The next call reloads the game exactly as it was left.
- **Shared database**: Any process with access to the same database sees the same game. Multiple service instances can drive the same game.
- **Singleton registration**: The engine holds no per-game state and can be registered as a singleton in the dependency-injection container.

The demo includes a database inspector that shows you the exact rows the engine writes on each call.

## Deterministic dealing with ICardShuffler

By default, every hand is shuffled at random, excluding every card already dealt to a seat or on the board. To replay recorded hand histories, run tests with predetermined sequences, or script a tutorial, use an `ICardShuffler`:

```csharp
services.AddTabletopCardEngine(tce => tce.AddHoldem(
    builder.Configuration.GetSection("TabletopCardEngine:Holdem"),
    db => db.UseHoldemSqlite(connectionString),
    options => options.WithCardShuffler(myShuffler)));
```

The shuffler is called twice per hand: once per seat for hole cards, and once for the board:

```csharp
public interface ICardShuffler
{
    ValueTask<IReadOnlyList<Card>> GetHoleCardsAsync(
        Guid gameId, int handNo, byte seatNo, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<Card>> GetDeckCardsAsync(
        Guid gameId, int handNo, CancellationToken cancellationToken);
}
```

**Important:** `GetDeckCardsAsync` is called again before each street change (flop, turn, river), so it must return the same sequence every time for the same hand. The engine does not retain the cards; your shuffler owns the data.

Engine options are per-container, so two instances in two containers can deal differently side by side — useful when one engine replays hand histories and another plays live hands.

## Optimistic concurrency with ExpectedVersion

`GameData.Version` starts at `1` when the game is created and increments by one on every call that saves. `GetGame` does not change it, so reading the game multiple times returns the same version.

To guard against concurrent writes, set `ExpectedVersion` on any action:

```csharp
var game = await engine.ApplyAsync(
    HoldemAction.GetGame(gameId), ct);

var result = await engine.ApplyAsync(
    HoldemAction.Play(gameId, PlayerActionType.Call) 
    { 
        ExpectedVersion = game.Version 
    }, ct);

if (!result.IsSuccess && result.Errors.Any(e => e.Code == HoldemErrorCode.Conflict))
{
    // Another action changed the game; reload and decide
    var latest = await engine.ApplyAsync(
        HoldemAction.GetGame(gameId), ct);
}
```

When `ExpectedVersion` is set and no longer matches the game's current version, the call fails with `HoldemErrorCode.Conflict` before any handler runs and before anything is persisted. Two writers racing without `ExpectedVersion` will also get a `Conflict` — one wins, one loses — because the engine detects the concurrent save.

The engine never retries and takes no locks. It is the caller's job to reload and decide whether to retry after a conflict. In an HTTP service this pattern maps to ETag / If-Match: every response includes an ETag with the version, and the client passes it back as an If-Match header on the next write.

## Read more

- **[The hand ledger](hand-ledger.md)** — Every action recorded, how `Amount` accumulates, and how pots are calculated from the ledger.
- **[Legal actions and bet sizing](evaluations.md)** — Understanding the evaluations offered at each decision, and why `Call` and `Raise` windows are on different bases.
- **[Heads-up play](heads-up.md)** — Two-player poker: position rules, the big blind's option, and how stacks below a blind are handled.
