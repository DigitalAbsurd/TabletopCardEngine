# Public API

This is the reviewable inventory of what `TabletopCardEngine.dll` exposes. Everything else in the
assembly (`Holdem/Data/`, `Holdem/Engine/`, `Holdem/Mappers/`, `Holdem/ActionHandlers/`) is `internal`; the tests and the
demo reach it through `InternalsVisibleTo`, which package validation ignores.

## How the baseline is enforced

The library project sets `EnablePackageValidation` and points `PackageValidationBaselinePath` at
[`eng/api-baseline/TabletopCardEngine.1.0.0-baseline.nupkg`](https://github.com/DigitalAbsurd/TabletopCardEngine/tree/main/eng/api-baseline), a package built
from the tree and committed. The gate is the plain command, with no extra properties:

```
dotnet pack src/TabletopCardEngine -c Release
```

It compares the freshly built assembly with the one inside the baseline package and fails with an
ApiCompat diagnostic on a break: `CP0001` for a removed type, `CP0002` for a removed member, and the
related `CP` rules for changed signatures, visibility or base types. `dotnet build` does not run
package validation; only `pack` does, which is why it is one of the gates.

What it does not catch: ApiCompat compares the binary surface, so a change that only breaks source
compatibility, such as removing a parameter's default value, passes. Review that by hand, against the
signatures below. A break that the gate reports, or a source break it misses, is a major version under
[versioning.md](versioning.md).

The baseline is a committed file rather than a published version because there is no published
version yet. Once 1.0.0 is on nuget.org, see [After 1.0.0 is on nuget.org](#after-100-is-on-nugetorg).

## The baseline is frozen

The rule for 1.x: **no breaking change; additions are allowed.** Package validation runs ApiCompat in
its default, non-strict mode, which reports removed and changed members (`CP0001`, `CP0002`, ...) and
not additions, so:

- An additive change needs **no** re-pack of any baseline. Regenerate the inventory below and nothing else:

  ```
  dotnet pack src/TabletopCardEngine -c Release
  pwsh ./scripts/list-public-api.ps1 -Update
  ```

- A `CP` diagnostic means the change is a break. It is not done in 1.x, and it is never "fixed" by
  re-packing the baseline. Do not set `EnableStrictModeForBaselineValidation` (it would report
  additions too), and never add a `CompatibilitySuppressions.xml` or `ApiCompatGenerateSuppressionFile`:
  a suppressed break is a baseline that no longer means anything.
- `ApiBaselineFreezeTests` pins the SHA-256 of every file in `eng/api-baseline/`, so a break hidden by
  a re-pack is a red test. Changing a pinned hash is a maintainer decision recorded in the CHANGELOG,
  never something an agent does.
- Members added after the freeze are not in the baselines, so they are not guarded until a release
  that contains them becomes the baseline. Once 1.0.0 is on nuget.org, the project switches to
  `PackageValidationBaselineVersion` (#313), which retires `ApiBaselineFreezeTests` together with
  `eng/api-baseline/`.

Every baseline in [`eng/api-baseline/`](https://github.com/DigitalAbsurd/TabletopCardEngine/tree/main/eng/api-baseline) holds one `lib/net10.0` asset, because the packages target
`net10.0` only. Package validation compares assets per target framework, so moving the packages to
another framework makes `dotnet pack` fail with `PKV006` ("Target framework net10.0 is no longer
supported in the latest version") before it compares a single member. That is the one case where
every baseline would be re-packed without a surface change; it, too, changes the pinned hashes and is
a maintainer decision.

## After 1.0.0 is on nuget.org

For the first release after the stable 1.0.0 is published on nuget.org, follow these steps:

1. In each of the six projects, replace `PackageValidationBaselinePath` with
   `<PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion>`. From then on the
   baseline is the last stable version on nuget.org, raised after each stable minor.

2. Add `EnablePackageValidation` and `PackageValidationBaselineVersion` to
   `TabletopCardEngine.OpenTelemetry` and remove its "never shipped" comment.

3. Delete `eng/api-baseline/` and the re-pack instructions that point at it: this section, fact 25 in
   [`task-run.md`](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/task-run.md#engine-facts), and the [`scripts/list-public-api.ps1`](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/scripts/list-public-api.ps1) help if it mentions the folder.

4. A surface change then needs no re-pack. A break against the published version fails `dotnet pack`
   with the same `CP` diagnostics, and it is a major version (see `versioning.md`).

This transition happens in the `v1.0 · NuGet packages & public launch` milestone.

### Enums are open

New members may be added to the public enums (`GameType`, `BettingStructure`, `HandActionType`,
`PlayerActionType`, `HoldemErrorCode`, `HandRankType`, `Street`, `HoldemSchemaState`,
`HoldemSchemaFailure`) in a minor release. Existing members never change value or meaning. A
consumer's `switch` over them needs a default arm.

## The provider packages

`TabletopCardEngine.Sqlite`, `TabletopCardEngine.PostgreSql`, `TabletopCardEngine.SqlServer` and
`TabletopCardEngine.InMemory` each have their own baseline,
`eng/api-baseline/TabletopCardEngine.<Provider>.1.0.0-baseline.nupkg` in
[`eng/api-baseline/`](https://github.com/DigitalAbsurd/TabletopCardEngine/tree/main/eng/api-baseline), enforced the same way by
`dotnet pack src/TabletopCardEngine.<Provider> -c Release`. Each provider's public surface is one
class of `UseHoldem*` overloads (`HoldemSqliteDbContextOptionsBuilderExtensions`,
`HoldemPostgreSqlDbContextOptionsBuilderExtensions`, `HoldemSqlServerDbContextOptionsBuilderExtensions`,
`HoldemInMemoryDbContextOptionsBuilderExtensions`); migrations and design-time factories are
`internal`. The baselines are packed without validation, because there is nothing to compare the
first one with (history, not to be run again; see [The baseline is frozen](#the-baseline-is-frozen)):

```
dotnet pack src/TabletopCardEngine.Sqlite -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
dotnet pack src/TabletopCardEngine.PostgreSql -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
dotnet pack src/TabletopCardEngine.SqlServer -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
dotnet pack src/TabletopCardEngine.InMemory -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
```

## The ASP.NET Core package

`TabletopCardEngine.AspNetCore` has its own baseline,
[`eng/api-baseline/TabletopCardEngine.AspNetCore.1.0.0-baseline.nupkg`](https://github.com/DigitalAbsurd/TabletopCardEngine/tree/main/eng/api-baseline),
enforced the same way by `dotnet pack src/TabletopCardEngine.AspNetCore -c Release`. Its public surface so far is
`HoldemHostingBuilderExtensions`, `HoldemHealthChecksBuilderExtensions`,
`HoldemHostOptions` and `HoldemEndpointRouteBuilderExtensions` (later additions do not re-pack the baseline). The baseline is packed without validation, because there is nothing to compare
the first one with (history, not to be run again; see [The baseline is frozen](#the-baseline-is-frozen)):

```
dotnet pack src/TabletopCardEngine.AspNetCore -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
```

## The OpenTelemetry package

`TabletopCardEngine.OpenTelemetry` has its own baseline,
[`eng/api-baseline/TabletopCardEngine.OpenTelemetry.1.0.0-baseline.nupkg`](https://github.com/DigitalAbsurd/TabletopCardEngine/tree/main/eng/api-baseline),
enforced the same way by `dotnet pack src/TabletopCardEngine.OpenTelemetry -c Release`. Its public surface is
`TabletopCardEngineInstrumentationExtensions` with the `AddTabletopCardEngineInstrumentation()` overloads for
`TracerProviderBuilder`, `MeterProviderBuilder` and `IOpenTelemetryBuilder`; see ADR-0002 for the architecture.
The baseline is packed without validation, because there is nothing to compare
the first one with (history, not to be run again; see [The baseline is frozen](#the-baseline-is-frozen)):

```
dotnet pack src/TabletopCardEngine.OpenTelemetry -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
```

The inventory below covers the core package only.

## Inventory

Generated by [`scripts/list-public-api.ps1`](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/scripts/list-public-api.ps1) from the built
assembly and its XML documentation; do not edit it by hand. Each line is the member's signature and
the first sentence of its doc comment (a positional record property uses the record's `<param>`).
Compiler-generated record members (`Equals`, `GetHashCode`, `ToString`, `PrintMembers`,
`Deconstruct`, `EqualityContract`, `<Clone>$`, `==`, `!=`) are left out: they follow from the record
declaration, and package validation checks them all the same.

<!-- BEGIN GENERATED by scripts/list-public-api.ps1 -->

### `TabletopCardEngine`

#### `Card : IParsable<Card>` (record)

One playing card.

- `Card(CardType cardType, CardSuit cardSuit)` — Creates a card; the only way to build one, so every instance has a defined rank and suit.
- `CardType CardType { get; }` — The card's rank.
- `CardSuit CardSuit { get; }` — The card's suit.
- `string ToString()` — Rank then suit, lowercase — e.g. "ah" (ace of hearts), "tc" (ten of clubs), "2s".
- `static Card Parse(string s)` — Parses a card from exactly two characters, rank and suit in either order, case-insensitively — `"ah"`, `"ha"` and `"AH"` all give the ace of hearts.
- `static bool TryParse(string? s, out Card? card)` — Parses a card like `Parse` but reports failure instead of throwing.

#### `CardSuit` (enum)

A card's suit.

- `Club = 0` — Clubs.
- `Diamond = 1` — Diamonds.
- `Heart = 2` — Hearts.
- `Spade = 3` — Spades.

#### `CardType` (enum)

A card's rank.

- `Ace = 1` — Ace.
- `Two = 2` — Two.
- `Three = 3` — Three.
- `Four = 4` — Four.
- `Five = 5` — Five.
- `Six = 6` — Six.
- `Seven = 7` — Seven.
- `Eight = 8` — Eight.
- `Nine = 9` — Nine.
- `Ten = 10` — Ten.
- `Jack = 11` — Jack.
- `Queen = 12` — Queen.
- `King = 13` — King.

#### `TabletopCardEngineBuilder` (sealed class)

The engine's registration, handed to the delegate of `AddTabletopCardEngine`.

- `IServiceCollection Services { get; }` — The container the games are registered into.

#### `TabletopCardEngineServiceCollectionExtensions` (static class)

Registers the engine with a dependency-injection container.

- `static IServiceCollection AddTabletopCardEngine(IServiceCollection services, Action<TabletopCardEngineBuilder> configure)` — Registers the engine, naming the engine first and each game second: `services.AddTabletopCardEngine(tce => tce.AddHoldem(...))`.

#### `TabletopCardEngineTelemetry` (static class)

The telemetry sources declared by the Holdem engine for OpenTelemetry hosts.

- `const string ActivitySourceName` — The name of the `ActivitySource` the engine publishes to.
- `const string MeterName` — The name of the `Meter` the engine publishes to.

### `TabletopCardEngine.Holdem`

#### `BettingStructure` (enum)

How bets and raises are sized in a game.

- `NoLimit = 1` — No-Limit: any bet or raise from the minimum up to the whole stack.

#### `GameType` (enum)

The poker variant a game is played in.

- `Holdem = 1` — Texas Hold'em: two hole cards per seat and five community cards.

#### `HandActionType` (enum)

What the hand's ledger records for one action.

- `SmallBlind = 1` — The forced small-blind post, placed by the engine when a hand starts.
- `BigBlind = 2` — The forced big-blind post, placed by the engine when a hand starts.
- `Fold = 3` — The seat gives up its claim on the pot.
- `Check = 4` — The seat stays in without adding chips.
- `Call = 5` — The seat matches the standing wager.
- `Raise = 6` — The seat raises the street wager to the recorded total.
- `Bet = 8` — An opening wager on a street where nothing has been bet.
- `AllIn = 9` — The seat has committed its whole stack.

#### `HandRankType` (enum)

The display category of a made hand, ascending in strength.

- `None = 0` — No hand has been evaluated.
- `HighCard = 1` — No pair or better.
- `Pair = 2` — One pair.
- `TwoPair = 3` — Two pair.
- `Trips = 4` — Three of a kind.
- `Straight = 5` — Five cards in sequence.
- `Flush = 6` — Five cards of one suit.
- `FullHouse = 7` — Three of a kind plus a pair.
- `FourOfAKind = 8` — Four of a kind.
- `StraightFlush = 9` — Five suited cards in sequence.

#### `HoldemBuilderExtensions` (static class)

Adds Texas Hold'em to the engine's registration.

- `static TabletopCardEngineBuilder AddHoldem(TabletopCardEngineBuilder builder, Action<DbContextOptionsBuilder> dbOptionsAction, Action<HoldemOptionsBuilder>? configure = null)` — Registers `IHoldemTable` as a singleton over an EF Core context factory.

#### `HoldemError` (sealed record)

One reason an action was rejected.

- `HoldemError(HoldemErrorCode Code, string Message)` — One reason an action was rejected.
- `HoldemErrorCode Code { get; init; }` — The stable, machine-readable reason.
- `string Message { get; init; }` — The localised, human-readable text for `Code`.

#### `HoldemErrorCode` (enum)

Machine-readable reason an action was rejected.

- `GameAlreadyExists = 1` — A game with the requested id already exists.
- `CurrentHandEnded = 2` — The current hand is completed, so no player action can be applied to it.
- `PreviousHandNotEnded = 3` — The previous hand is not completed, so a new hand cannot start yet.
- `InvalidAction = 4` — The player action's type is not a defined `PlayerActionType` member.
- `UnsupportedGameOptions = 5` — The game's `GameType` and `BettingStructure` is not a supported combination.
- `InvalidGameOptions = 6` — A game option is out of range: seat count, small blind or first dealer seat.
- `InvalidSeat = 7` — A seat to join is out of range, repeated, already occupied, or has a non-positive buy-in.
- `GameAlreadyStarted = 8` — The game already has a hand dealt, or was already started, so it cannot be started again.
- `GameNotStarted = 9` — No hand has been dealt yet, so the action needs `StartGame` first.
- `GameFinished = 10` — The game was finished, so only reading it back is allowed.
- `Conflict = 11` — The game was changed by another action since the caller read it, so nothing was applied.
- `GameNotFound = 12` — No game with the requested id exists.
- `IllegalAction = 13` — The action is not one the seat to act may take now, or its size is outside the window offered.

#### `HoldemResult<T>` (sealed class)

The outcome of one engine call: a value on success, or the errors that rejected the action.

- `bool IsSuccess { get; }` — Whether the action was applied.
- `bool IsFailed { get; }` — Whether the action was rejected.
- `IReadOnlyList<HoldemError> Errors { get; }` — The reasons the action was rejected; empty on success.
- `T? Value { get; }` — The value of a successful call.

#### `HoldemSchemaException : InvalidOperationException, ISerializable` (sealed class)

The database cannot be used as the engine is configured: a deployment fault, not a game result, which is why it is an exception rather than a `HoldemErrorCode`.

- `HoldemSchemaException()` — Creates an exception with no message and no status.
- `HoldemSchemaException(string? message)` — Creates an exception with a message and no status.
- `HoldemSchemaException(string? message, Exception? innerException)` — Creates an exception with a message, the exception that caused it, and no status.
- `HoldemSchemaFailure Failure { get; }` — Why the database cannot be used.
- `HoldemSchemaStatus? Status { get; }` — The schema status that led to the failure, or `null` when it was not read.

#### `HoldemSchemaFailure` (enum)

Machine-readable reason a `HoldemSchemaException` was thrown.

- `UnsupportedProvider = 1` — The configured EF Core provider is not one of the four the engine supports.
- `MigrationsNotFound = 2` — The provider is relational but no migrations were found, because it was configured with the plain EF Core call instead of the `UseHoldem*` call of the provider's engine package.
- `DatabaseIsNewer = 3` — The database was migrated by a newer engine than this one.
- `MigrationRequired = 4` — The database's schema is behind the engine's and has to be migrated first.

#### `HoldemSchemaState` (enum)

How a database's schema compares with the one the engine expects.

- `UpToDate = 1` — Every migration the engine knows of has been applied.
- `MigrationRequired = 2` — The engine has migrations the database has not applied yet; call `MigrateAsync`.
- `NewerThanEngine = 3` — The database was migrated by a newer engine than this one, which must not write to it.
- `NotVersioned = 4` — The provider keeps no schema history (InMemory), so there is no version to compare.

#### `HoldemSchemaStatus` (sealed record)

What `GetSchemaStatusAsync` found.

- `HoldemSchemaStatus(HoldemSchemaState State, string ProviderName, int EngineSchemaVersion, int? DatabaseSchemaVersion, string? DatabaseEngineVersion, IReadOnlyList<string> PendingMigrations)` — What `GetSchemaStatusAsync` found.
- `HoldemSchemaState State { get; init; }` — How the database's schema compares with the engine's.
- `string ProviderName { get; init; }` — The EF Core provider name, such as `Microsoft.EntityFrameworkCore.Sqlite`.
- `int EngineSchemaVersion { get; init; }` — The schema version this engine expects.
- `int? DatabaseSchemaVersion { get; init; }` — The schema version recorded in the database, or `null` when none is recorded.
- `string? DatabaseEngineVersion { get; init; }` — The version of the engine that last migrated the database, or `null` when none is recorded.
- `IReadOnlyList<string> PendingMigrations { get; init; }` — The migrations the database has not applied yet, in the order they would be applied; empty when there are none.

#### `ICardShuffler` (interface)

Supplies pre-determined cards instead of a real shuffle, which is what makes replaying a recorded hand history possible.

- `ValueTask<IReadOnlyList<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo, CancellationToken cancellationToken)` — The hole cards to deal to one seat, in dealt order.
- `ValueTask<IReadOnlyList<Card>> GetDeckCardsAsync(Guid gameId, int handNo, CancellationToken cancellationToken)` — The board cards for the hand, in dealt order.

#### `IHoldemSchemaManager` (interface)

The public way to create and inspect the engine's database schema, so that a host never has to reach the engine's internal database context to do it.

- `Task MigrateAsync(CancellationToken cancellationToken = default)` — Brings the schema up to date.
- `Task<HoldemSchemaStatus> GetSchemaStatusAsync(CancellationToken cancellationToken = default)` — Reports how the database's schema compares with the one this engine expects, without changing anything.

#### `IHoldemTable` (interface)

A Hold'em table: the single entry point for game commands, and every game action goes through `ApplyAsync`.

- `Task<HoldemResult<GameData>> ApplyAsync(HoldemAction action, CancellationToken cancellationToken = default)` — Applies one action to a game and returns the game as it now stands.

#### `PlayerActionType` (enum)

What a caller may ask a seat to do.

- `Fold = 3` — The seat gives up its claim on the pot.
- `Check = 4` — The seat stays in without adding chips; legal only when there is nothing to call.
- `Call = 5` — The seat matches the standing wager.
- `Raise = 6` — The seat raises the street wager.
- `Bet = 8` — An opening wager on a street where nothing has been bet.
- `AllIn = 9` — Puts the seat's whole stack in: a call when the stack does not exceed the amount to call, otherwise a raise to the full stack.

#### `Street` (enum)

A betting round.

- `PreFlop = 0` — Before the flop; no community cards.
- `Flop = 3` — Three community cards.
- `Turn = 4` — Four community cards.
- `River = 5` — Five community cards.
- `Showdown = 6` — Betting is over and the remaining hands are compared.
- `Finished = 7` — The hand is settled and the pot has been paid out.

### `TabletopCardEngine.Holdem.Dto`

#### `GameData` (record)

Everything the engine knows about a game, as returned by every call.

- `GameData(Guid GameId, GameOptionData Options, HandData? HandData, bool IsFinished = false, int Version = 0)` — Everything the engine knows about a game, as returned by every call.
- `Guid GameId { get; init; }` — Identifier of the game.
- `GameOptionData Options { get; init; }` — The rules the game was created with.
- `HandData? HandData { get; init; }` — The current hand, or `null` when no hand has been dealt yet.
- `bool IsFinished { get; init; }` — Whether `FinishGame` has closed the game; after that only reading it back is allowed.
- `int Version { get; init; }` — How many times the game has been saved, starting at `1` on creation.

#### `GameOptionData` (sealed record)

The fixed rules a game is created with; they never change once the game exists.

- `GameOptionData()` — The implicit parameterless constructor, for object initializers.
- `GameType GameType { get; init; }` — The poker variant.
- `BettingStructure BettingStructure { get; init; }` — How bets are sized.
- `int MaxPlayersCount { get; init; }` — How many seats the table has; must lie within the engine's configured seat limits, which are 2 to 6 in this release.
- `decimal InitialSmallBlind { get; init; }` — The small blind of every hand in this release; the big blind is twice this.
- `byte InitialDealerSeatNo { get; init; }` — Where the button sits for the first hand.

#### `HandData` (record)

The current hand: who is in it, what is on the board, and what the seat to act may do.

- `HandData(int No, IReadOnlyList<SeatData> Seats, byte DealerSeatNo, byte SmallBlindSeatNo, byte BigBlindSeatNo, byte NextPlayerSeatNo, Street Street, decimal StreetPot, decimal Pot, IReadOnlyList<Card> CommunityCards, IReadOnlyList<PlayerActionEvaluationData> Evaluations)` — The current hand: who is in it, what is on the board, and what the seat to act may do.
- `int No { get; init; }` — Hand number within the game, ascending from 1.
- `IReadOnlyList<SeatData> Seats { get; init; }` — Every occupied seat, ascending by seat number.
- `byte DealerSeatNo { get; init; }` — Where the button is.
- `byte SmallBlindSeatNo { get; init; }` — The seat that posted the small blind, or `0` before the hand starts.
- `byte BigBlindSeatNo { get; init; }` — The seat that posted the big blind, or `0` before the hand starts.
- `byte NextPlayerSeatNo { get; init; }` — The seat to act, or `0` when it is nobody's turn.
- `Street Street { get; init; }` — The street now in progress.
- `decimal StreetPot { get; init; }` — Contributions made on the current street only.
- `decimal Pot { get; init; }` — Everything contributed on prior streets — `StreetPot` is not included.
- `IReadOnlyList<Card> CommunityCards { get; init; }` — The board, in dealt order.
- `IReadOnlyList<PlayerActionEvaluationData> Evaluations { get; init; }` — What `NextPlayerSeatNo` may legally do; empty when it is nobody's turn.
- `decimal SmallBlind { get; init; }` — The nominal small blind of this hand.
- `decimal BigBlind { get; init; }` — The nominal big blind of this hand.
- `IReadOnlyList<PlayerActionData> Actions { get; init; }` — Every action of this hand in the order it was taken (ascending `No`), the engine's blind posts included.
- `PlayerActionData GetLastAction()` — The most recent action taken by anyone in this hand.

#### `HandResultData` (record)

What a seat walked away from a finished hand with.

- `HandResultData(decimal Net, uint Strength, HandRankType RankType, string? Description = null)` — What a seat walked away from a finished hand with.
- `decimal Net { get; init; }` — Chips won minus chips wagered over the whole hand; negative for a loser.
- `uint Strength { get; init; }` — A comparable hand-strength value — only meaningful against another seat's value in the same hand.
- `HandRankType RankType { get; init; }` — The display category of the made hand.
- `string? Description { get; init; }` — Human-readable name of the made hand's category, such as "Two pair"; `null` when the seat did not reach showdown (it folded, or the hand ended because everyone else folded).

#### `HoldemAction` (abstract record)

The one command type the engine accepts.

- `HoldemAction(Guid GameId)` — The one command type the engine accepts.
- `Guid GameId { get; init; }` — The game the action applies to.
- `int? ExpectedVersion { get; init; }` — The `Version` the caller last saw, or `null` to skip the check.
- `static HoldemActionGetGame GetGame(Guid gameId)` — Reads the game back without changing anything.
- `static HoldemActionCreateGame CreateGame(GameOptionData options, Guid? gameId = null)` — Creates a game with the given rules, generating an identifier when none is supplied.
- `static HoldemActionStartGame StartGame(Guid gameId, PlayerJoinData[] joinSeats)` — Starts a created game: seats the players and deals hand 1.
- `static HoldemActionStartHand StartHand(Guid gameId, PlayerJoinData[]? joinSeats = null)` — Deals the next hand of a started game, optionally seating players first.
- `static HoldemActionPlay Play(Guid gameId, PlayerActionType actionType, decimal size = 0)` — Plays one action for the seat whose turn it is.
- `static HoldemActionFinishGame FinishGame(Guid gameId)` — Closes the game, between hands only.

#### `HoldemActionCreateGame : HoldemAction` (record)

Creates a game.

- `HoldemActionCreateGame(Guid GameId, GameOptionData Options)` — Creates a game.
- `GameOptionData Options { get; init; }` — The rules the game is created with.

#### `HoldemActionFinishGame : HoldemAction` (record)

Closes the game, between hands only: it fails with `PreviousHandNotEnded` while a hand is in progress.

- `HoldemActionFinishGame(Guid GameId)` — Closes the game, between hands only: it fails with `PreviousHandNotEnded` while a hand is in progress.

#### `HoldemActionGetGame : HoldemAction` (record)

Reads the game back without changing anything.

- `HoldemActionGetGame(Guid GameId)` — Reads the game back without changing anything.

#### `HoldemActionPlay : HoldemAction` (record)

Plays one action for the seat whose turn it is.

- `HoldemActionPlay(Guid GameId, PlayerActionType ActionType, decimal Size)` — Plays one action for the seat whose turn it is.
- `PlayerActionType ActionType { get; init; }` — What the seat does.
- `decimal Size { get; init; }` — For `Raise` or `Bet`, the street total to raise or bet to — the same absolute basis as the raise bounds in `Evaluations`.

#### `HoldemActionStartGame : HoldemAction` (record)

Starts a created game: seats the players and deals hand 1.

- `HoldemActionStartGame(Guid GameId, IReadOnlyList<PlayerJoinData> JoinSeats)` — Starts a created game: seats the players and deals hand 1.
- `IReadOnlyList<PlayerJoinData> JoinSeats { get; init; }` — The seats to fill, in any order; the engine sorts them by seat number.

#### `HoldemActionStartHand : HoldemAction` (record)

Deals the next hand, optionally seating players in empty seats first.

- `HoldemActionStartHand(Guid GameId)` — Deals the next hand, optionally seating players in empty seats first.
- `IReadOnlyList<PlayerJoinData> JoinSeats { get; init; }` — Players who sit down between hands, each into a seat that has never been occupied in this game; topping up an occupied seat is not supported.

#### `PlayerActionData` (record)

A single action a seat has taken, as recorded in the hand's ledger.

- `PlayerActionData(Street Street, HandActionType ActionType, decimal Size, int No)` — A single action a seat has taken, as recorded in the hand's ledger.
- `Street Street { get; init; }` — The street the action was taken on.
- `HandActionType ActionType { get; init; }` — What the seat did.
- `decimal Size { get; init; }` — The size the caller asked for; `0` for actions that carry no size.
- `int No { get; init; }` — Position of this action in the hand, ascending from 1.
- `byte SeatNo { get; init; }` — The seat that took the action.
- `decimal Amount { get; init; }` — The seat's cumulative wager on this street after the action: what a blind actually posted, the total a call or check stands at, the street total a raise or bet went to; `0` for a fold.

#### `PlayerActionEvaluationData` (record)

One action the seat to act may legally take, with its sizing window.

- `PlayerActionEvaluationData(PlayerActionType PlayerActionType, decimal MinValue = 0, decimal MaxValue = 0)` — One action the seat to act may legally take, with its sizing window.
- `PlayerActionType PlayerActionType { get; init; }` — The action this window describes.
- `decimal MinValue { get; init; }` — Smallest legal amount, on the basis described above.
- `decimal MaxValue { get; init; }` — Largest legal amount, on the basis described above.

#### `PlayerJoinData` (record)

A player taking a seat, together with the stack they bring to it.

- `PlayerJoinData(string Name, byte SeatNo, decimal BuyInAmount)` — A player taking a seat, together with the stack they bring to it.
- `string Name { get; init; }` — Display name of the player; must be 1 to 64 characters and not null or whitespace-only.
- `byte SeatNo { get; init; }` — The seat to occupy.
- `decimal BuyInAmount { get; init; }` — The stack the player sits down with; must have at most two decimal places.

#### `SeatData` (record)

One seat as it stands in the current hand.

- `SeatData(byte SeatNo, string Name, decimal StartHandStack, decimal Stack, decimal? BetSize = null, IReadOnlyList<Card>? Hole = null, PlayerActionData? LastAction = null, HandResultData? HandResult = null)` — One seat as it stands in the current hand.
- `byte SeatNo { get; init; }` — The seat number.
- `string Name { get; init; }` — Display name of the player in the seat.
- `decimal StartHandStack { get; init; }` — The stack this seat sat down with at the start of the hand.
- `decimal Stack { get; init; }` — The stack right now — `StartHandStack` minus everything wagered this hand.
- `decimal? BetSize { get; init; }` — The seat's wager on the street now on the clock — its highest cumulative amount in the ledger for that street, the same street `StreetPot` counts; `null` when the seat has no action on that street yet (a new street opens with everyone at `null`).
- `IReadOnlyList<Card>? Hole { get; init; }` — The seat's hole cards, or `null` when they are not visible to the caller.
- `PlayerActionData? LastAction { get; init; }` — The most recent action this seat took, or `null` if it has not acted.
- `HandResultData? HandResult { get; init; }` — Set once the hand is over; `null` while it is still running.
- `bool IsActive { get; }` — Whether the seat is still contesting the pot — it has chips and cards, or it is all in.
- `bool IsFolded { get; }` — Whether the seat has folded out of the hand.
- `bool IsAllIn { get; }` — Whether the seat has committed its whole stack.

### `TabletopCardEngine.Holdem.Options`

#### `HoldemOptionsBuilder` (sealed class)

Configures the engine from `HoldemBuilderExtensions.AddHoldem`.

- `HoldemOptionsBuilder WithSettings(Action<HoldemSettings> configureSettings)` — Adjusts the validation limits applied to every game.
- `HoldemOptionsBuilder WithCardShuffler(ICardShuffler cardShuffler)` — Replaces the random shuffle with pre-determined cards, which is what makes replaying a recorded hand history possible.

#### `HoldemSettings` (sealed class)

Validation limits applied to every game the engine runs.

- `HoldemSettings()` — The implicit parameterless constructor, for object initializers.
- `int GamePlayersMinCount { get; set; }` — Fewest seats a hand may be dealt to.
- `int GamePlayersMaxCount { get; set; }` — Most seats a hand may be dealt to.
- `bool EnableSensitiveCardLogging { get; set; }` — When `true`, the engine logs every seat's hole cards and the board at Trace level.

<!-- END GENERATED -->
