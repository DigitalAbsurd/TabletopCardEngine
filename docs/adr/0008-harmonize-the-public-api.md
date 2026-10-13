# ADR-0008: Harmonize the public API

- **Status:** Accepted
- **Date:** 2026-10-13
- **Issue:** [#411](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/411), milestone
  `v1.0 · Harmonize the public API`

## Context

After the rename ([ADR-0007](0007-rename-to-tabletopcardengine.md)), the brand is `TabletopCardEngine`,
but the public API is still anchored on `HoldemEngine`: a consumer writes `services.AddHoldemEngine(...)`,
resolves `HoldemEngine` and calls `ApplyHoldemActionAsync`, configures `HoldemEngineOptionsBuilder`,
maps `MapHoldemEngineApi` and adds `AddHoldemEngineCheck`. ADR-0007 §3 kept those names on purpose
("renaming these would be a design change, not a rename"). The maintainer has now made that design
change: the engine is for tabletop card games played with friends, Hold'em is the first game and others
(Omaha, Durak) are far plans, so the API has to read as *the TabletopCardEngine, with Hold'em
registered in it*.

Nothing is on nuget.org and the repository is private, so the break is free now and never again.

## Decision

### 1. Root namespace stays `TabletopCardEngine`

It is not shortened to `TCE`: the .NET naming guidelines use `Company.Product` without abbreviations, a
package ID prefix matches its namespace (`TabletopCardEngine.*` is the ID prefix), and `TCE` is
ambiguous and hard to search. `TCE` stays the abbreviation for environment variables, the database
schema and diagnostic IDs only. The root namespace holds only game-agnostic types: `Card`, `CardSuit`,
`CardType`, `TabletopCardEngineBuilder`, `TabletopCardEngineServiceCollectionExtensions` and
`TabletopCardEngineTelemetry`.

### 2. Each game has its own namespace

Hold'em lives in `TabletopCardEngine.Holdem`, `TabletopCardEngine.Holdem.Dto` and
`TabletopCardEngine.Holdem.Options`. That includes `GameType`, `BettingStructure`, `Street`,
`HandActionType`, `HandRankType`, `PlayerActionType`, `ICardShuffler`, `HoldemResult<T>`,
`HoldemError`, `HoldemErrorCode`, `IHoldemSchemaManager` and the `HoldemSchema*` types. The packages
do not split: Hold'em stays in the `TabletopCardEngine` package, because a later move to a
`TabletopCardEngine.Holdem` package can keep binary compatibility with `[TypeForwardedTo]`.

### 3. Registration is engine first, game second

```csharp
services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemInMemory("holdem"), holdem => holdem.WithCardShuffler(shuffler)));
```

`TabletopCardEngineBuilder` is a sealed class in the root namespace with an internal constructor and
one public property, `IServiceCollection Services`, so that a game's package can extend it. `AddHoldem`
is an extension method on it in `TabletopCardEngine.Holdem`. `AddHoldemEngine` is removed, not kept as
an alias. Hold'em may be registered once per service collection; a second `AddHoldem` throws
`InvalidOperationException`. A future game adds an `AddXxx` extension on the same builder.

### 4. The game service is a table

`HoldemEngine` becomes the public interface `IHoldemTable` with one method,
`Task<HoldemResult<GameData>> ApplyAsync(HoldemAction action, CancellationToken cancellationToken = default)`,
implemented by an internal sealed `HoldemTable`. A future game gets `IXxxTable`. The internal
`Engine/HandTable` keeps its name; it is not public and does not collide.

### 5. The schema manager stays per game

`IHoldemSchemaManager`, `HoldemSchemaStatus`, `HoldemSchemaState`, `HoldemSchemaFailure` and
`HoldemSchemaException` keep their names and move to `TabletopCardEngine.Holdem`. Each game migrates
its own context.

### 6. Each game has its own `DbContext` and its own database schema

Hold'em's is `HoldemDbContext` (unchanged) in the schema `tce_holdem`, replacing `tce`: on PostgreSQL
and SQL Server the tables, `__TceSchema` and `__TceMigrationsHistory` live in `tce_holdem`. SQLite has
no schemas, so there every table is named `<schema>_<table>`: `tce_holdem_Game`, `tce_holdem_Hand`, …,
`tce_holdem___TceSchema`, `tce_holdem___TceMigrationsHistory`. The provider packages keep each game's
migrations in a namespace of their own, `TabletopCardEngine.<Provider>.Migrations.Holdem`.

### 7. The ASP.NET Core package follows

`HoldemEngineHostingServiceCollectionExtensions.AddHoldemEngine(IConfiguration, …)` becomes an
`AddHoldem(IConfiguration, …)` extension on `TabletopCardEngineBuilder`;
`HoldemEngineHostOptions` → `HoldemHostOptions`; `MapHoldemEngineApi` → `MapHoldemApi` (the
`/api/holdem` prefix is unchanged); `AddHoldemEngineCheck` → `AddHoldemCheck`, default name
`tabletopcardengine-holdem`; the OpenAPI tag becomes `Holdem`. The hosts bind the section
`TabletopCardEngine:Holdem` (environment `TabletopCardEngine__Holdem__*`), which amends ADR-0007 §3a;
the connection string `ConnectionStrings:TabletopCardEngine` is unchanged, because all games share one
database and are kept apart by schema.

### 8. The name mapping

One table, old → new → issue, with every row from sections 2–7, including the internal renames:

| Old | New | Issue |
|---|---|---|
| `AddHoldemEngine` | `AddTabletopCardEngine` | Public registration root |
| `HoldemEngineBuilder` | `TabletopCardEngineBuilder` | Public builder class |
| `HoldemEngineServiceCollectionExtensions` | `TabletopCardEngineServiceCollectionExtensions` | Public extension point |
| `AddHoldemEngine` (game extension) | `AddHoldem` | Game registration extension |
| `HoldemEngine` (public class) | `IHoldemTable` | Game service interface |
| `HoldemEngine` (internal) | `HoldemTable` | Game service implementation |
| `HoldemEngineOptions` | `HoldemOptions` | Internal options class |
| `HoldemEngineOptionsBuilder` | `HoldemOptionsBuilder` | Internal options builder |
| `HoldemEngineSettings` | `HoldemSettings` | Internal settings class |
| `HoldemEngineTelemetry` | `TabletopCardEngineTelemetry` | Public telemetry class |
| `Diagnostics/HoldemEngineLog.cs` | `Diagnostics/HoldemLog.cs` | Internal diagnostics |
| `Diagnostics/HoldemEngineActivity.cs` | `Diagnostics/HoldemActivity.cs` | Internal diagnostics |
| `Diagnostics/HoldemEngineMetrics.cs` | `Diagnostics/HoldemMetrics.cs` | Internal diagnostics |
| `HoldemEngineApiMarker` | `HoldemApiMarker` | Internal API marker |
| `HoldemEngineHostingServiceCollectionExtensions.AddHoldemEngine` | `HoldemHostingBuilderExtensions.AddHoldem` | ASP.NET Core service collection |
| `HoldemEngineHostingEndpointRouteBuilderExtensions.MapHoldemEngineApi` | `HoldemEndpointRouteBuilderExtensions.MapHoldemApi` | ASP.NET Core endpoint mapping |
| `HoldemEngineHealthChecksBuilderExtensions.AddHoldemEngineCheck` | `HoldemHealthChecksBuilderExtensions.AddHoldemCheck` | ASP.NET Core health checks |
| `HoldemEngineHostOptions` | `HoldemHostOptions` | ASP.NET Core host options |
| `GameType` | `GameType` (move to `TabletopCardEngine.Holdem`) | Game type enumeration |
| `BettingStructure` | `BettingStructure` (move to `TabletopCardEngine.Holdem`) | Betting structure enumeration |
| `Street` | `Street` (move to `TabletopCardEngine.Holdem`) | Street enumeration |
| `HandActionType` | `HandActionType` (move to `TabletopCardEngine.Holdem`) | Hand action type enumeration |
| `HandRankType` | `HandRankType` (move to `TabletopCardEngine.Holdem`) | Hand rank type enumeration |
| `PlayerActionType` | `PlayerActionType` (move to `TabletopCardEngine.Holdem`) | Player action type enumeration |
| `ICardShuffler` | `ICardShuffler` (move to `TabletopCardEngine.Holdem`) | Card shuffler interface |
| `HoldemResult<T>` | `HoldemResult<T>` (move to `TabletopCardEngine.Holdem`) | Result type |
| `HoldemError` | `HoldemError` (move to `TabletopCardEngine.Holdem`) | Error type |
| `HoldemErrorCode` | `HoldemErrorCode` (move to `TabletopCardEngine.Holdem`) | Error code enumeration |
| `IHoldemSchemaManager` | `IHoldemSchemaManager` (move to `TabletopCardEngine.Holdem`) | Schema manager interface |
| `HoldemSchemaStatus` | `HoldemSchemaStatus` (move to `TabletopCardEngine.Holdem`) | Schema status enumeration |
| `HoldemSchemaState` | `HoldemSchemaState` (move to `TabletopCardEngine.Holdem`) | Schema state enumeration |
| `HoldemSchemaFailure` | `HoldemSchemaFailure` (move to `TabletopCardEngine.Holdem`) | Schema failure enumeration |
| `HoldemSchemaException` | `HoldemSchemaException` (move to `TabletopCardEngine.Holdem`) | Schema exception type |

### 9. What is deliberately not renamed

`HoldemAction*`, `HoldemResult`, `HoldemError`, `HoldemErrorCode`, `HoldemSchema*`,
`IHoldemSchemaManager`, `UseHoldem*`, `HoldemDbContext`, the `Holdem*Request` types, the `/api/holdem`
routes, and every telemetry signal name (ActivitySource and Meter `TabletopCardEngine`, the span, tag
and metric names in `docs/observability.md`). A per-game tag on those signals is a later, additive
change. `GameOptionData.GameType` stays as the Hold'em-family variant switch; `docs/roadmap.md` still
lists Omaha as out of scope, and this ADR only lays the foundation.

### 10. Exception 1: baselines are re-packed inside this milestone

This supersedes ADR-0007 §4's "this one re-pack and no other" and the rule in `docs/public-api.md` that
changing a pinned hash is never an agent's doing, **for the issues of this milestone only**. Each issue
that breaks a package re-packs only that package's baseline with
`-p:Version=1.0.0-baseline -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false`,
after checking that every `CP` diagnostic names a row of the mapping table, re-pins the hash in
`ApiBaselineFreezeTests`, and lands as a `refactor!:` commit with a `BREAKING CHANGE:` footer. After
the milestone the rule in `docs/public-api.md` holds again unchanged.

### 11. Exception 2: `InitialCreate` is edited in place again

For the same reason as ADR-0007 §5: no database outside the tests has been migrated. The schema change
from `tce` to `tce_holdem` is applied in place in `InitialCreate` for each provider, rather than
through a new migration.

### 12. Exception 3: the trial pre-releases are deleted

The GitHub release `v1.0.0-preview.2` and the tags `v1.0.0-preview.1` and `v1.0.0-preview.2` are
deleted, `CHANGELOG.md` is regenerated without them, and the ladder of `docs/release-process.md`
restarts at `v1.0.0-preview.1`. This is a one-off exception to "a tag is never moved or reused": both
tags were private, carried an API that no longer exists, and no package from them was published.

## Consequences

- **This ADR lays the foundation for a multi-game platform.** The `TabletopCardEngine.Holdem` namespace
  makes room for future games without collision; each game registers itself on the same builder.
- **The registration pattern is extensible.** `services.AddTabletopCardEngine(tce => tce.AddHoldem(...).AddOmaha(...))`,
  not `services.AddHoldemEngine(...).AddOmahaEngine(...)`, reads clearly and leaves the builder open
  for a future game.
- **Schema isolation is per game.** Each game has its own migrations, its own context, and its own
  schema namespace (`tce_holdem`, `tce_omaha`, etc.), so games do not collide in the database.
- **A future game gets its own ASP.NET Core extensions.** The pattern of `HoldemHostingBuilderExtensions`,
  `HoldemEndpointRouteBuilderExtensions`, and `HoldemHealthChecksBuilderExtensions` repeats for each
  game, with no new root-level API.
- **This ADR is a breaking change at v1.0.** The refactored API is released as v1.0.0, not as a patch
  on v1.0.0-previewN, because the public surface changed in every incompatible direction. An adopter of
  the trial pre-releases must migrate to the new names.
