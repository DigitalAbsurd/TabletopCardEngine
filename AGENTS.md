# AGENTS.md

This file gives guidance to AI coding assistants working in this repository. It is written for
any agent, not for one product. An assistant that reads its own file name, such as `CLAUDE.md`,
can point that file here.

It describes the codebase. The workflow lives in two other documents:
[docs/task-run.md](docs/task-run.md) is the execution contract for implementing an issue (gates,
what "done" means, the git and pull request protocol, and the engine facts), and
[docs/tasks-planning.md](docs/tasks-planning.md) explains how requirements become issues.

## Commands

```bash
dotnet build
```

```bash
dotnet test
```

The public API gate: packing compares every package against its committed baseline in
`eng/api-baseline/` and fails on a removed or changed public member (`CP0001`, `CP0002`, ...).
`dotnet build` does not run it. See [docs/public-api.md](docs/public-api.md) for the inventory and
how to change the baseline.

```bash
dotnet pack OpenHoldemEngine.Packages.slnf -c Release
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

The ASP.NET Core package's tests are a separate assembly and run the same way:

```bash
dotnet run --project src/OpenHoldemEngine.AspNetCore.Tests -- --filter "FullyQualifiedName~Package_DependsOnly"
```

Test-only environment variables (read in `TestEngineHost`, `PhhFixture` and `Persistence/TestDatabase`, and `OHE_TEST_BROWSER_GATE` in `src/OpenHoldemEngine.Demo.BrowserTests`):

- `OHE_TEST_SQLITE_DB3_FILE_NAME` — write the SQLite DB to a file instead of `:memory:`, so the schema/rows can be inspected after a run.
- `OHE_TEST_SQL_DEBUG=true` — log EF SQL with sensitive data to the debug output (DEBUG builds only).
- `OHE_TEST_PHH_HAND_LIMIT` — how many hands of the PHH corpus `PhhPluribusReplay_Test` replays (default 500; `0` replays all 10 000, about four minutes). A capped run takes an evenly spaced sample, not the first N hands, so it still reaches every table in the corpus.
- `OHE_TEST_BROWSER_GATE=1` — enables the Playwright layout gate in `src/OpenHoldemEngine.Demo.BrowserTests`, which is skipped without it (`OHE_TEST_BROWSER_GATE=1 dotnet test src/OpenHoldemEngine.Demo.BrowserTests`). CI skips the gate on a pull request labelled `browser-gate-local`, meaning it was run locally; the rules are in [docs/task-run.md](docs/task-run.md#gates).
- `OHE_TEST_CONTAINERS=1` — runs the `PostgreSql` and `SqlServer` rows of the provider matrix, in one Testcontainers container per kind for the whole run, with a database of its own per test. **Needs Docker.** Without it those rows report inconclusive, so a plain `dotnet test` stays green on a machine without Docker. CI's `providers` job sets it; the rules are in [docs/task-run.md](docs/task-run.md#gates).

## What this is

A .NET 10 class library modelling Texas Hold'em cash games. There is no executable — consumers register it with `services.AddHoldemEngine(dbOptionsAction, configure)` and drive everything through a single method:

```csharp
Task<Result<GameData>> HoldemEngine.ApplyHoldemActionAsync(HoldemAction action, CancellationToken ct)
```

### Scope of v1

No-Limit Texas Hold'em, cash games, 2 to 6 seats, no antes, no rake. Other structures (Pot-Limit,
Fixed-Limit) are v2, and `GameOptionData` (`GameType`, `BettingStructure`) is where they will plug in.
The engine enforces this: see fact 23 in [docs/task-run.md](docs/task-run.md).

## Architecture

### Command → handler → aggregate → projection

`ApplyHoldemActionAsync` ([HoldemEngine.cs](src/OpenHoldemEngine/HoldemEngine.cs)) is the whole public flow, and every call repeats it:

1. `IHoldemActionHandler.CreateHandlerForAction` — a `switch` on the `HoldemAction` record type picks the handler. New action types must be added both to `Dto/HoldemActions.cs` and to that switch.
2. `LoadGameWithCurrentHandAsync` reloads the `Game` aggregate **plus only the latest `Hand`** and its collections. If the game does not exist it returns a placeholder `new Game { Id = Guid.Empty }` already attached to the context — that placeholder is how `CreateGameActionHandler` distinguishes "new" from "already exists" (`game.Id != default`), and how `HoldemEngine.ApplyCoreAsync` rejects an unknown game with `GameNotFound` for every other action, once and before the `ExpectedVersion` check, so nothing is saved. The lifecycle is `CreateGame` → `StartGame` → (`StartHand` | player actions)* → `FinishGame`; `GetGame` is allowed in every state, and once `Game.IsFinished` is set every other action fails with `GameFinished`. The default arm of `CreateHandlerForAction` returns a handler that fails with `InvalidAction` rather than throwing.
3. The handler mutates the EF entity graph and returns a `HoldemError?` (`null` is success), which `ApplyHoldemActionAsync` wraps into the public `HoldemResult<GameData>`; each `HoldemError` carries a stable `HoldemErrorCode`. A failed result short-circuits before `SaveChangesAsync`, so validation failures leave nothing persisted.
4. `Mappers/GameDataMapper` projects the whole aggregate into the `GameData` DTO returned to the caller.

There is no long-lived in-memory game object. **The database is the state**; each call rehydrates from it.

### Concurrency

`Game.Version` is an optimistic concurrency token (`IsConcurrencyToken()`; SQLite has no `rowversion`, so the application increments it). `CreateGame` sets it to `1`, every other call that saves does `game.Version++` first so the `UPDATE Game ... WHERE Version = @old` is issued even when only child rows changed, and `GetGame` neither saves nor increments. It is exposed as `GameData.Version`. `HoldemAction.ExpectedVersion` (optional `init` property, ignored by `CreateGame`) is checked against the loaded version before any handler runs. Either that mismatch or a lost race at save time returns `HoldemErrorCode.Conflict`. On a relational provider the save runs in one explicit transaction that issues the conditional `UPDATE Game SET Version = @new WHERE Version = @old` as a statement of its own *before* any child row (`HoldemEngine.SaveVersionFirstAsync`): Npgsql and SqlClient batch a save, and in one batch a losing writer hit a duplicate `HandAction` key before EF read the `UPDATE`s row count. A writer whose update matches no row rolls back and gets `Conflict`; one still racing waits on the row lock the update takes and then matches nothing. A `DbUpdateConcurrencyException` from the save still maps to `Conflict` too. On a non-relational provider (InMemory, which applies inserts before updates and has no transactions, so a lost race would otherwise surface as a duplicate-key `ArgumentException`), the engine also re-reads `Game.Version` just before saving and returns `Conflict` when it moved; nothing is persisted, and the engine never retries and takes no locks: the caller reloads with `GetGame` and decides. Any other `DbUpdateException` stays unhandled, as a real fault.

### Rules live in Engine/, in domain terms

Betting rules, blinds, pot/side-pot splitting, showdown and street progression are implemented in-repo under `Engine/` — there is no external rules package. On every call `HandTableFactory.Build` rehydrates the persisted entities into a transient `HandTable` (seats, board, deck, whose turn it is), `HandRunner.StartHand` / `ApplyAction` mutates it in place, and `BettingRules.LegalActions` answers what a seat may currently do. The *result* is written back as rows (`HandAction`, `HandDeckCard`, `HandPlayerEndState`); `HandTable` itself is always derived and never stored, so changing how entities convert to it changes game behaviour.

`Engine/HandTable.cs` collapses what used to be three separate NPoker types (`GameState` + `Table` + `Seats`) into one; `Engine/TableSeat.cs` similarly collapses `Player` + `PayInfo` + a per-street action-history list into scalars, because nothing ever needed more than "the last action" and "the biggest wager this street" out of that history. Occupied seats only, ascending by `SeatNo` — no empty-seat padding, no `Dummy#N` players. `Engine/CardDeck.cs` is either a real shuffle (excluding every card already dealt) or a fixed sequence from `ICardShuffler`. `Engine/BettingRules.cs`, `Engine/HandRunner.cs` and `Engine/PotDistribution.cs` hold the actual rules (legal actions and bet sizing, the betting/street state machine, and side-pot construction, respectively) and are pure functions/static classes over a `HandTable` — no cloning, no defensive copying, because the table itself is thrown away at the end of every request.

Hand strength is supplied by `Engine/HandEvaluator.cs`, an in-repo evaluator with two methods — `EvaluateStrength` (a single comparable value, for ranking hands against each other) and `EvaluateRankType` (the display category, read back out of that same value). It replaced the `HandEvaluator` NuGet package, which declares no license and derives from Keith Rule's GPLv3 port of poker-eval — don't bring that package, or another port of poker-eval, back. The strength packs the category into bits 20 and up and the five deciding ranks four bits apiece below it, ranks stored as 1 (two) to 13 (ace) so that 0 can mean "no card" when fewer than five are dealt. `HandEvaluatorTests` pins it against the textbook five-card distribution and the 7 462 distinct hand classes.

### Card formats

The DB/`Card.ToString()` format (`"ah"`, rank-then-suit lowercase) is canonical everywhere in this codebase. PHH writes `"Ah"` (rank upper / suit lower); `PhhCards` lower-cases it before `Card.FromString`.

### Deterministic dealing via ICardShuffler

`ICardShuffler` is an optional hook (`HoldemEngineOptionsBuilder.WithCardShuffler`). When set:

- `HandDealer` (shared by `StartGameActionHandler` and `StartHandActionHandler`) pulls hole cards per seat (by real `SeatNo`, ascending) and feeds them into `CardDeck.Predetermined` before `HandRunner.StartHand`.
- `PlayActionHandler` re-fetches the remaining board cards into a fresh `CardDeck.Predetermined` before each street change.

When null, `CardDeck.Shuffled` draws a real shuffle that excludes every card already dealt this hand (hole cards and board), so a random rebuild can never repeat a card that is already on the table or in someone's hand. This hook is what makes replaying recorded hand histories possible.

### Observability

The engine publishes logs, traces, and metrics as part of its public contract. Log events (via `ILogger`), span tags (via `ActivitySource`), and metric instruments are defined in `Diagnostics/` and `HoldemEngineTelemetry`, and documented in `docs/observability.md` with one row per event/tag/instrument. Event IDs are stable and never reused; renaming, removing, or changing an ID, tag meaning, or unit is a breaking change equivalent to a removed API surface. The sync test `ObservabilityDocsTests` enforces that every event, tag, and instrument appears in the document. See `docs/task-run.md` fact 26 for the architecture and the signals' scope.

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
- **`PlayerActionType` is what a command may carry, `HandActionType` is what the ledger records.** A caller can request Fold, Check, Call, Raise, Bet or AllIn; `HandAction.ActionType` (and so `PlayerActionData.ActionType`) stores the requested action as a `HandActionType`, which adds `SmallBlind` and `BigBlind`, posted by the engine. `PlayActionHandler` maps one to the other in one place, and an undefined `PlayerActionType` value is rejected with `HoldemErrorCode.InvalidAction`. `AllIn` is always legal and commits the whole stack: `PlayActionHandler` turns it into a call (possibly for less) when `Stack + StreetWager <= CallTo`, otherwise a raise to that total, and the ledger records `AllIn`. A recorded `AllIn` is a raise kind on rehydration only when its `AddAmount > 0`. Nothing is ever corrected into a fold: `BettingRules.CorrectAction` throws on an action `PlayActionHandler` should have rejected.
- **The engine rejects an illegal action, it never corrects one.** A Fold, Check, Call or Raise that `HandData.Evaluations` does not offer, or a Raise outside its window, fails with `HoldemErrorCode.IllegalAction` and saves nothing; `Evaluations` and that check share `BettingRules.Evaluate` (fact 29 in [docs/task-run.md](docs/task-run.md)).
- **`HandAction` is the ledger.** `Amount` is the seat's *cumulative* wager for that street, `AddAmount` the increment, `Size` the caller's requested size. All pot maths (`EntityExtensions.GetPot`, `GetCurrentStreetPot`, `GameDataMapper`) works by grouping actions on `(SeatNo, Street)` and taking `Max(Amount)`. In `GameData`, `StreetPot` is the current street's contributions and `Pot` is everything from prior streets.
- **`Raise` evaluations are absolute, `Call` evaluations are an increment.** In `HandData.Evaluations`, a `Call`'s `MinValue`/`MaxValue` are "how much more to put in", but a `Raise`'s `MinValue`/`MaxValue` are both "raise the street total to this" — the same basis `PlayActionHandler.CalculateBetAmount` expects for `HoldemActionPlay.Size` on a raise. Mixed on purpose: a "call" UI wants a top-up amount, a "raise to" slider wants an absolute.
- **Pot splits round to the cent.** `PotDistribution` rounds each winner's share to two decimal places and gives whatever is left over to the last winner, rather than dividing the pot exactly — an unrounded `pot / winnerCount` repeats forever in decimal for most winner counts and can need most of `decimal`'s ~28-29 significant digits, which then forces silent rounding (differently for different seats, since they rarely have the same number of stack digits) the next time that share is added to a stack. Rounding up front keeps every downstream amount small enough that this never happens.
- **Engine options belong to the container.** Each `AddHoldemEngine` call creates its own `HoldemEngineOptions`, has the builder write into it, and registers it as a singleton instance in that `IServiceCollection`. The handlers never read options at all: `IHoldemActionHandler.CreateHandlerForAction(action, options)` hands the `ICardShuffler?` to `StartGameActionHandler`, `StartHandActionHandler` and `PlayActionHandler` through their constructors. Two engines built from two containers can deal differently side by side, and that is what the demo does for replay (see `DemoReplayEngine`). Both test assemblies run with `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`, so a test that shares state with another is a defect to fix, not a reason to serialise the assembly.
- **Every direct dependency is in `THIRD-PARTY-NOTICES.md`.** The notices' package inventory lists only the packages a project references directly (non-`autoReferenced` entries of `project.frameworks.*.dependencies`); transitive ones get no row. `ThirdPartyNoticesTests` reads each project's `obj/project.assets.json` and fails in four cases. A direct package has no row. A row names a package nothing references directly. A row's license differs from the nuspec's. Any resolved package, *transitive ones included*, has a license outside the permissive allowlist (MIT, Apache-2.0, BSD-2/3, and the legacy Microsoft .NET Library License), with one standing exception: the SNI runtime, documented in [ADR-0003](docs/adr/0003-database-provider-licences.md). Adding or bumping a package means re-running `pwsh ./scripts/audit-licenses.ps1` and pasting its tables in; a new vendored asset (a JS library under `wwwroot/lib`, a data file) needs a row in the notices by hand. Anything outside the allowlist needs a legal review, not a wider allowlist; the PostgreSQL entry is also recorded in ADR-0003.
- **The schema comes from per-provider migrations.** Each supported database has an `OpenHoldemEngine.<Provider>` package (so far `OpenHoldemEngine.Sqlite`) whose `Migrations/` are generated from `HoldemDbContext.OnModelCreating` (composite keys throughout) and applied by `IHoldemSchemaManager.MigrateAsync`; the `__OheSchema` row is model seed data, so each migration inserts it. An entity shape change needs the model configuration updated by hand *and* a migration for every provider — the rules are in [docs/persistence.md](docs/persistence.md), and `SqlitePackageTests.Sqlite_MigratedSchema_HasNoPendingModelChanges` fails when the model and the snapshot disagree.
- **Visibility**: all `Data/`, `Engine/`, and `Mappers/` types, handlers, and converters are `internal`; the public surface is `HoldemEngine`, `ServiceCollectionExtensions`, `Dto/`, `Card` + the enums (all in the root `OpenHoldemEngine` namespace, so `Data/` holds internal entities only), `ICardShuffler`, the schema entry point (`IHoldemSchemaManager`, `HoldemSchemaStatus`, `HoldemSchemaState`, `HoldemSchemaFailure`, `HoldemSchemaException`), and `Options/`. The separate `OpenHoldemEngine.OpenTelemetry` package adds exactly one public type, `OpenHoldemEngineInstrumentationExtensions` (the `AddOpenHoldemEngineInstrumentation()` overloads for `TracerProviderBuilder`, `MeterProviderBuilder`, and `IOpenTelemetryBuilder`, see [ADR-0002](docs/adr/0002-opentelemetry-instrumentation-package.md)); it references `OpenTelemetry.Api` and `OpenTelemetry.Api.ProviderBuilderExtensions` only, and the core references no OpenTelemetry package at all. The `OpenHoldemEngine.AspNetCore` package references the shared framework (`Microsoft.AspNetCore.App`) and the core only, with no `PackageReference` ([ADR-0004](docs/adr/0004-aspnetcore-integration-package.md), guarded by `AspNetCorePackageTests`); it reaches internals through the core's `InternalsVisibleTo` and has its own API baseline. The persistence packages `OpenHoldemEngine.Sqlite`, `OpenHoldemEngine.PostgreSql`, `OpenHoldemEngine.SqlServer`, and `OpenHoldemEngine.InMemory` each add exactly one public type (`HoldemSqliteDbContextOptionsBuilderExtensions`, `HoldemPostgreSqlDbContextOptionsBuilderExtensions`, `HoldemSqlServerDbContextOptionsBuilderExtensions`, and `HoldemInMemoryDbContextOptionsBuilderExtensions` respectively, with overloads where applicable), and each has its own API baseline; their migrations (where applicable) and design-time factories are `internal`, and they reach the internal `HoldemDbContext` through the core's `InternalsVisibleTo` for each package name. The core references no database provider. Tests reach internals via `InternalsVisibleTo`.
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

Shared harness: `TestEngineHost.Setup` spins up a real DI container over SQLite (`:memory:` with a kept-alive connection) through `UseHoldemSqlite` and creates the schema with `IHoldemSchemaManager.MigrateAsync()`, so every engine test runs on the shipped migrations; `TestCardShuffler` is the `ICardShuffler` that feeds pre-determined cards into the cheat deck.

The provider matrix lives in `Persistence/`. `TestDatabase.Configure(TestDatabaseKind, out lifetime)` gives each test its own isolated database of one kind: `Sqlite` (a kept-alive `:memory:` connection), `InMemory` (a `Guid`-named store), or `PostgreSql` / `SqlServer`, which run in Docker under `OHE_TEST_CONTAINERS=1` and report inconclusive without it. `TestEngineHost.Setup` takes an optional last `database` parameter and defaults to SQLite. On SQLite and InMemory alike run the smoke subset (`MultiHandSession_KeepsTheLedgerBalanced`, and `ExpectedVersion_Stale_ReturnsConflict` / `GetGame_Twice_KeepsTheVersion` in `ConcurrencyTests`), the race twins `TwoWriters_SecondGetsConflict` (SQLite file) and `TwoWriters_SecondGetsConflict_InMemory`, and `ProviderSchemaTests` (model check, re-migrate keeps the data). The upgrade test and its guard `Upgrade_EveryMigrationAfterTheFirstHasAFixture` cover the relational packages: every migration after `InitialCreate` needs one `IUpgradeFixture` under `Persistence/Upgrades/` (rule 5 in [docs/persistence.md](docs/persistence.md)). `TestDatabaseKind` is `public` because an `internal` enum cannot be a `[DataRow]` parameter of a public test method (CS0051).

[PhhReplayTests.cs](src/OpenHoldemEngine.Tests/PhhReplayTests.cs) is the integration workhorse. It replays recorded hand histories from an open dataset — the Pluribus subset of the [PHH dataset](https://github.com/uoftcprg/phh-dataset), MIT, vendored as an embedded gzipped `.phhs` under `Resources/phh/` (see its [README](src/OpenHoldemEngine.Tests/Resources/phh/README.md) for provenance and `scripts/pack-phh-fixture.ps1` to regenerate). 10 000 six-handed no-limit hands at 50/100, every hole card recorded and `finishing_stacks` as the oracle.

- `Phh/` holds the reader (`PhhReader` over Tomlyn, `PhhActionParser` for the action grammar, `PhhCards`), the per-hand deck (`PhhHandDeck`) and the driver (`PhhReplayRunner`).
- Each hand is replayed as its **own game** — the corpus resets stacks every hand and stacks cannot be topped up mid-game. Seat mapping is straight out of the PHH spec: `pI` → `SeatNo = I`, `InitialDealerSeatNo = PlayerCount`, so `p1` is the small blind and the last player is on the button. The first hand is dealt by `StartGame`, which sorts `JoinSeats` by `SeatNo` itself, so the order the runner passes them in does not matter.
- PHH `cbr X` maps to `PlayerActionType.Raise` with `Size = X` — both mean "bring the street wager up to X". `cc` becomes `Check` or `Call` depending on what `HandData.Evaluations` offers.
- Asserted per hand: button/blind seats, start stacks, hole cards per seat, actor order at every decision, street progression against the board, and `StartHandStack + Net == finishing_stacks`.
- `PhhCorpus_ParsesAndIsFullySupported` asserts that **no** hand is filtered out as unsupported, so a reader regression cannot silently shrink the corpus.

A second corpus, `handhq-headsup.phhs.gz`, covers heads-up: 2 351 real \$0.25/\$0.50 PokerStars hands, 2 145 of them replayable. It is a **structural** oracle only — handhq records no `finishing_stacks` and rakes its pots, so `AssertHandResult` compares stacks only when a hand has them, and `IsSupported` no longer requires them. `PhhHeadsUpCorpus_ParsesAndIsMostlySupported` pins the supported count instead of asserting nothing is filtered. Heads-up seating in PHH is the one place the spec's two rules collide: the *last* player posts the small blind, not `p1` (see `PhhReplayRunner.BlindSeatsFor` and the fixture README).
- `PhhReplayRunner` throws away and rebuilds its database every 250 hands. Every hand is its own game, so rows only pile up, and an unbounded run slows down badly as the in-memory database grows.

[MultiHandSessionTests.cs](src/OpenHoldemEngine.Tests/MultiHandSessionTests.cs) covers what per-hand replay cannot: one 200-hand game with stacks carrying over, a seeded action policy, and invariants as the oracle (chip conservation, button rotation, blind placement, no negative stacks, `Sum(Net) == 0`). It runs twice, `[DataRow(6)]` and `[DataRow(2)]`, so the heads-up positions get the same 200 hands.

[HeadsUpTests.cs](src/OpenHoldemEngine.Tests/HeadsUpTests.cs) spells the heads-up rules out one at a time on a scripted deck: who posts, who is on the clock street by street, the big blind's option, button alternation, and the two short-stack cases where a blind post is capped at the stack.

Object comparisons use `actual.ShouldAsJToken().BeEquivalentToAsJToken(expected)` (FluentAssertions.Json, `ObjectAssertionsExtensions`) so records compare structurally with enums as strings. Keep actual-then-expected order. Note that FluentAssertions is pinned at 6.1.0, so the newer spellings (`AllSatisfy`, `BeGreaterThanOrEqualTo`) do not exist.
