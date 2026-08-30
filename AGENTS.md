# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build
```

```bash
dotnet test
```

Style is enforced at build time, so `dotnet build` fails on a violation. To fix what is
auto-fixable instead of reading the diagnostics one by one:

```bash
dotnet format OpenHoldemEngine.sln
```

```bash
dotnet format OpenHoldemEngine.sln --verify-no-changes
```

Run a single test — **`dotnet test --filter` is silently ignored here** (MSTest.Sdk 3.6.1 / Microsoft.Testing.Platform); the filter must be passed to the test app itself:

```bash
dotnet run --project src/OpenHoldemEngine.Tests -- --filter "FullyQualifiedName~HoldemAction_StartGame"
```

```bash
dotnet run --project src/OpenHoldemEngine.Tests -- --list-tests
```

Test-only environment variables (read in `TestEngineHost` and `PhhFixture`):

- `OHE_TEST_SQLITE_DB3_FILE_NAME` — write the SQLite DB to a file instead of `:memory:`, so the schema/rows can be inspected after a run.
- `OHE_TEST_SQL_DEBUG=true` — log EF SQL with sensitive data to the debug output (DEBUG builds only).
- `OHE_TEST_PHH_HAND_LIMIT` — how many hands of the PHH corpus `PhhPluribusReplay_Test` replays (default 500; `0` replays all 10 000, about four minutes). A capped run takes an evenly spaced sample, not the first N hands, so it still reaches every table in the corpus.

## What this is

A .NET 8 class library modelling Texas Hold'em cash games. There is no executable — consumers register it with `services.AddHoldemEngine(dbOptionsAction, configure)` and drive everything through a single method:

```csharp
Task<Result<GameData>> HoldemEngine.ApplyHoldemActionAsync(HoldemAction action, CancellationToken ct)
```

## Architecture

### Command → handler → aggregate → projection

`ApplyHoldemActionAsync` ([HoldemEngine.cs](src/OpenHoldemEngine/HoldemEngine.cs)) is the whole public flow, and every call repeats it:

1. `IHoldemActionHandler.CreateHandlerForAction` — a `switch` on the `HoldemAction` record type picks the handler. New action types must be added both to `Dto/HoldemActions.cs` and to that switch.
2. `LoadGameWithCurrentHandAsync` reloads the `Game` aggregate **plus only the latest `Hand`** and its collections. If the game does not exist it returns a placeholder `new Game { Id = Guid.Empty }` already attached to the context — that placeholder is how `CreateGameActionHandler` distinguishes "new" from "already exists" (`game.Id != default`).
3. The handler mutates the EF entity graph and returns `FluentResults.Result`. A failed result short-circuits before `SaveChangesAsync`, so validation failures leave nothing persisted.
4. `Mappers/GameDataMapper` projects the whole aggregate into the `GameData` DTO returned to the caller.

There is no long-lived in-memory game object. **The database is the state**; each call rehydrates from it.

### Rules live in Engine/, in domain terms

Betting rules, blinds, pot/side-pot splitting, showdown and street progression are implemented in-repo under `Engine/` — there is no external rules package. On every call `HandTableFactory.Build` rehydrates the persisted entities into a transient `HandTable` (seats, board, deck, whose turn it is), `HandRunner.StartHand` / `ApplyAction` mutates it in place, and `BettingRules.LegalActions` answers what a seat may currently do. The *result* is written back as rows (`HandAction`, `HandDeckCard`, `HandPlayerEndState`); `HandTable` itself is always derived and never stored, so changing how entities convert to it changes game behaviour.

`Engine/HandTable.cs` collapses what used to be three separate NPoker types (`GameState` + `Table` + `Seats`) into one; `Engine/TableSeat.cs` similarly collapses `Player` + `PayInfo` + a per-street action-history list into scalars, because nothing ever needed more than "the last action" and "the biggest wager this street" out of that history. Occupied seats only, ascending by `SeatNo` — no empty-seat padding, no `Dummy#N` players. `Engine/CardDeck.cs` is either a real shuffle (excluding every card already dealt) or a fixed sequence from `ICardShuffler`. `Engine/BettingRules.cs`, `Engine/HandRunner.cs` and `Engine/PotDistribution.cs` hold the actual rules (legal actions and bet sizing, the betting/street state machine, and side-pot construction, respectively) and are pure functions/static classes over a `HandTable` — no cloning, no defensive copying, because the table itself is thrown away at the end of every request.

Hand strength is supplied by `Engine/HandEvaluator.cs`, a singleton over the `HandEvaluator` (HoldemHand) package with two methods — `EvaluateStrength` (a single comparable value, for ranking hands against each other) and `EvaluateRankType` (the display category).

### Card formats

Two card string formats exist: the DB/`Card.ToString()` format (`"ah"`, rank-then-suit lowercase, canonical everywhere in this codebase) and HoldemHand's (`"Ah"`, rank upper / suit lower). `HandEvaluator.ToHoldemHandString` is the one place that converts between them.

### Deterministic dealing via ICardShuffler

`ICardShuffler` is an optional hook (`HoldemEngineOptionsBuilder.WithCardShuffler`). When set:

- `StartHandActionHandler` pulls hole cards per seat (by real `SeatNo`, ascending) and feeds them into `CardDeck.Predetermined` before `HandRunner.StartHand`.
- `PlayerActionHandler` re-fetches the remaining board cards into a fresh `CardDeck.Predetermined` before each street change.

When null, `CardDeck.Shuffled` draws a real shuffle that excludes every card already dealt this hand (hole cards and board), so a random rebuild can never repeat a card that is already on the table or in someone's hand. This hook is what makes replaying recorded hand histories possible.

### Conventions that bite

- **Heads-up changes the positions, not just the ring size.** With two funded seats the button posts
  the small blind, acts *first* pre-flop and *last* on every street after it. `ExcludeShortOfMoneySeats`
  assigns those two roles directly when `ActiveCount == 2` rather than deriving and then swapping them,
  because `StealFromPoorSeats` picks seats by what they can afford and swapping its answers would hand
  the big blind to a seat that only qualified for the small one. `DealStreet` recognises heads-up as
  `DealerSeatNo == SmallBlindSeatNo`.
- **A blind post is capped at the stack**, and the seat then goes all in (`PostBlind`). `HandAction.Amount`
  for a blind row is therefore what was posted, not the nominal blind — `Size` keeps the nominal one.
  Heads-up is where this happens routinely, and the two `HeadsUpTests` short-stack cases pin it down.
- **Seat numbering is 1-based everywhere**, including inside `Engine/` — there is no 0-based layer to cross, so there is no `+1` / `-1` boundary translation left in the codebase. `0` means "no seat" (nobody's turn, no dealer yet), the same sentinel the DB's `NextSeatNo` column already used.
- **`Street` is not sequential** — `Flop = 3, Turn = 4, River = 5` encode the community-card count, and code relies on it. `StreetExtensions.Next()` is the only place that knows the successor order; `StreetExtensions.BoardSize()` is the only place that turns a street into a card count.
- **`HandAction` is the ledger.** `Amount` is the seat's *cumulative* wager for that street, `AddAmount` the increment, `Size` the caller's requested size. All pot maths (`EntityExtensions.GetPot`, `GetCurrentStreetPot`, `GameDataMapper`) works by grouping actions on `(SeatNo, Street)` and taking `Max(Amount)`. In `GameData`, `StreetPot` is the current street's contributions and `Pot` is everything from prior streets.
- **`Raise` evaluations are absolute, `Call` evaluations are an increment.** In `HandData.Evaluations`, a `Call`'s `MinValue`/`MaxValue` are "how much more to put in", but a `Raise`'s `MinValue`/`MaxValue` are both "raise the street total to this" — the same basis `PlayerActionHandler.CalculateBetAmount` expects for `HoldemActionPlayerAction.Size` on a raise. Mixed on purpose: a "call" UI wants a top-up amount, a "raise to" slider wants an absolute.
- **Pot splits round to the cent.** `PotDistribution` rounds each winner's share to two decimal places and gives whatever is left over to the last winner, rather than dividing the pot exactly — an unrounded `pot / winnerCount` repeats forever in decimal for most winner counts and can need most of `decimal`'s ~28-29 significant digits, which then forces silent rounding (differently for different seats, since they rarely have the same number of stack digits) the next time that share is added to a stack. Rounding up front keeps every downstream amount small enough that this never happens.
- **`HoldemEngineOptions.Instance` is a process-wide singleton.** `HoldemEngineOptionsBuilder.Options` hands back that same instance, so configuring one engine mutates global state for all of them. This is why the test assembly is marked `[assembly: DoNotParallelize]`.
- **No EF migrations.** The schema comes from `HoldemDbContext.OnModelCreating` (composite keys throughout) plus `Database.EnsureCreated()`. Entity shape changes need no migration but do need the model configuration updated by hand.
- **Visibility**: all `Data/`, `Engine/`, and `Mappers/` types, handlers, and converters are `internal`; the public surface is `HoldemEngine`, `ServiceCollectionExtensions`, `Dto/`, `Card` + the enums, `ICardShuffler`, and `Options/`. Tests reach internals via `InternalsVisibleTo`.
- Validation messages are resource strings in `Resources/PackResource.resx`, keyed `HoldemValidation_*`.

## Code style

[.editorconfig](.editorconfig) is the single source of truth, and
[Directory.Build.props](Directory.Build.props) is what gives it teeth:
`EnforceCodeStyleInBuild` runs the `IDExxxx` style rules during compilation,
`AnalysisMode=Recommended` turns on the `CAxxxx` quality rules, and `TreatWarningsAsErrors`
turns every one of them into a build error. **A style violation fails `dotnet build`.** No
third-party analyzer packages are used — everything ships with the SDK.

What that means in practice:

- **English only, everywhere in the codebase.** Code, comments, commit messages, XML doc
  comments, `.editorconfig`/`.gitattributes`/`Directory.Build.props` annotations, and resource
  strings (`Resources/PackResource.resx`) are all English — no exceptions. This isn't caught by
  the analyzers above; it's a review-time rule.
- **Formatting** is one umbrella rule, `IDE0055`. Don't hand-tune whitespace; run `dotnet format`.
- **Naming** is `IDE1006` plus eleven `dotnet_naming_rule` entries. Private fields are
  `_camelCase` **whether or not they are static** (`_cards`, `_suitByChar`, `_raiseKinds`);
  `internal`/`public` fields, constants and enum members are PascalCase (`HandEvaluator.Instance`,
  `PhhCards.FullDeck`); parameters and locals are `camelCase` — including parameters of primary
  constructors, which is a place this codebase used to write `_options`.
  <br>One gap worth knowing: **the naming rules do not reject underscores.** Roslyn's `pascal_case`
  check only looks at the first letter, so `Probe_UnderscoreName` satisfies it. Underscores are
  caught by `CA1707` instead, which by default only sees externally visible members — widening it
  with `api_surface = all` is not an option, because it then fires on the `_camelCase` field
  convention itself. Underscores inside `internal`/`private` names are a review matter.
- **`var` is allowed but not required** (`IDE0007`/`IDE0008` are `silent`), and so are
  expression-bodied methods. Both were flipped to match how the code is actually written.
- **Braces are mandatory** on every `if`/`else`/loop, even a one-liner (`IDE0011`).
- **File-scoped namespaces** (`IDE0161`), `using` outside the namespace (`IDE0065`), namespace
  matching folder (`IDE0130`), explicit accessibility modifiers (`IDE0040`).
- **Encoding is UTF-8 with BOM, CRLF, final newline**, pinned by `.editorconfig` and
  [.gitattributes](.gitattributes).
- The library sets `GenerateDocumentationFile`, so **every public type and member needs an XML doc
  comment** — `CS1591` is an error. `Engine/` is the reference for tone: say why, not what the name
  already says. The test project generates the file too (it is what makes `IDE0005` run at build)
  but suppresses `CS1591`/`CS1573`.
- **Test method names keep their underscores.** `Subject_Scenario[_Expected]`
  (`HoldemAction_StartGame_ValidationAlreadyExists`) is the convention. MSTest needs the classes
  public, which puts their methods in `CA1707`'s sights, so a `[**/*Tests.cs]` section in
  `.editorconfig` turns that rule off — 18 errors without it. The pattern matches `*Tests.cs`
  only; helpers under `Phh/`, `TestEngineHost` and `TestCardShuffler` follow the normal rules.
- To reject a rule, set its severity in `.editorconfig` with a comment saying why. Don't reach for
  a blanket `NoWarn` in a `.csproj`.

## Tests

Shared harness: `TestEngineHost.Setup` spins up a real DI container over SQLite (`:memory:` with a kept-alive connection) and calls `EnsureDeleted()` / `EnsureCreated()`; `TestCardShuffler` is the `ICardShuffler` that feeds pre-determined cards into the cheat deck.

[PhhReplayTests.cs](src/OpenHoldemEngine.Tests/PhhReplayTests.cs) is the integration workhorse. It replays recorded hand histories from an open dataset — the Pluribus subset of the [PHH dataset](https://github.com/uoftcprg/phh-dataset), MIT, vendored as an embedded gzipped `.phhs` under `Resources/phh/` (see its [README](src/OpenHoldemEngine.Tests/Resources/phh/README.md) for provenance and `scripts/pack-phh-fixture.ps1` to regenerate). 10 000 six-handed no-limit hands at 50/100, every hole card recorded and `finishing_stacks` as the oracle.

- `Phh/` holds the reader (`PhhReader` over Tomlyn, `PhhActionParser` for the action grammar, `PhhCards`), the per-hand deck (`PhhHandDeck`) and the driver (`PhhReplayRunner`).
- Each hand is replayed as its **own game** — the corpus resets stacks every hand and `BuyIns` can only top a stack up. Seat mapping is straight out of the PHH spec: `pI` → `SeatNo = I`, `InitialDealerSeatNo = PlayerCount`, so `p1` is the small blind and the last player is on the button. `JoinSeats` must be passed in ascending seat order, because `StartHandActionHandler`'s `ResolvePreviousDealerSeatNo` walks that array for the very first hand.
- PHH `cbr X` maps to `PlayerActionType.Raise` with `Size = X` — both mean "bring the street wager up to X". `cc` becomes `Check` or `Call` depending on what `HandData.Evaluations` offers.
- Asserted per hand: button/blind seats, start stacks, hole cards per seat, actor order at every decision, street progression against the board, and `StartHandStack + Net == finishing_stacks`.
- `PhhCorpus_ParsesAndIsFullySupported` asserts that **no** hand is filtered out as unsupported, so a reader regression cannot silently shrink the corpus.

A second corpus, `handhq-headsup.phhs.gz`, covers heads-up: 2 351 real \$0.25/\$0.50 PokerStars hands, 2 145 of them replayable. It is a **structural** oracle only — handhq records no `finishing_stacks` and rakes its pots, so `AssertHandResult` compares stacks only when a hand has them, and `IsSupported` no longer requires them. `PhhHeadsUpCorpus_ParsesAndIsMostlySupported` pins the supported count instead of asserting nothing is filtered. Heads-up seating in PHH is the one place the spec's two rules collide: the *last* player posts the small blind, not `p1` (see `PhhReplayRunner.BlindSeatsFor` and the fixture README).
- `PhhReplayRunner` throws away and rebuilds its database every 250 hands. Every hand is its own game, so rows only pile up, and an unbounded run slows down badly as the in-memory database grows.

[MultiHandSessionTests.cs](src/OpenHoldemEngine.Tests/MultiHandSessionTests.cs) covers what per-hand replay cannot: one 200-hand game with stacks carrying over, a seeded action policy, and invariants as the oracle (chip conservation, button rotation, blind placement, no negative stacks, `Sum(Net) == 0`). It runs twice, `[DataRow(6)]` and `[DataRow(2)]`, so the heads-up positions get the same 200 hands.

[HeadsUpTests.cs](src/OpenHoldemEngine.Tests/HeadsUpTests.cs) spells the heads-up rules out one at a time on a scripted deck: who posts, who is on the clock street by street, the big blind's option, button alternation, and the two short-stack cases where a blind post is capped at the stack.

Object comparisons use `actual.ShouldAsJToken().BeEquivalentToAsJToken(expected)` (FluentAssertions.Json, `ObjectAssertionsExtensions`) so records compare structurally with enums as strings. Keep actual-then-expected order. Note that FluentAssertions is pinned at 6.1.0, so the newer spellings (`AllSatisfy`, `BeGreaterThanOrEqualTo`) do not exist.

### Known engine defects the tests pin down

- `HoldemActionStartHand.BuyIns` / `KickOffSeats` and `GameOptionData.AllowRebuy` are declared but never read by any handler, so mid-session buy-ins and kick-offs cannot be tested yet.
