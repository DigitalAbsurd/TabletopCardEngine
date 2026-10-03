---
---

# Observability

This document describes the engine's diagnostics: what logging, tracing and metrics it produces, the contract their names form, and how it surfaces errors and failures to the host.

## Wiring

The engine publishes logs, traces and metrics. Wire them up with OpenTelemetry:

**With the OpenTelemetry instrumentation package (recommended):**

```csharp
services
    .AddOpenTelemetry()
    .AddOpenHoldemEngineInstrumentation()
    .WithTracing(tracing => tracing.AddConsoleExporter())
    .WithMetrics(metrics => metrics.AddConsoleExporter());
```

**Alternative: with explicit calls:**

```csharp
services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddOpenHoldemEngineInstrumentation())
    .WithMetrics(metrics => metrics.AddOpenHoldemEngineInstrumentation());
```

**Without the package:**

For logs, add the `OpenHoldemEngine` category to the host's logger configuration:

```csharp
services.AddLogging(logging => logging.AddFilter("OpenHoldemEngine", LogLevel.Information));
```

For traces, add the activity source:

```csharp
services.AddOpenTelemetry().WithTracing(tracing => 
    tracing.AddSource(HoldemEngineTelemetry.ActivitySourceName));
```

For metrics, add the meter:

```csharp
services.AddOpenTelemetry().WithMetrics(metrics => 
    metrics.AddMeter(HoldemEngineTelemetry.MeterName));
```

**Logs:** The engine logs through `ILogger<HoldemEngine>`. When no `ILoggerFactory` is registered in the container, the engine logs nothing (it does not create its own factory).

**Metrics:** The engine publishes metrics through an `IMeterFactory`. When one is present, it calls `IMeterFactory.Create()` to get a meter with the `MeterName`. When absent, it falls back to a process-wide `Meter` that persists for the application's lifetime.

## Logs

Event ID numbers are stable parts of the contract and are never reused. The level rule is:

- **Warning**: a lost race (concurrency conflict).
- **Information**: a lifecycle change of a game or hand, and a rejected command (a caller-visible result).
- **Debug**: every applied command and every street change.
- **Trace**: reserved for the opt-in card events (see [Sensitive data](#sensitive-data)).

The engine logs nothing at Error or Critical: exceptions propagate to the host, which logs them.

| Event ID | Method | Level | Template | When |
|---|---|---|---|---|
| 1001 | `ActionApplied` | Debug | `{ActionType} applied to game {GameId}, now at version {Version}` | A command succeeded and changed the game |
| 1002 | `ActionRejected` | Information | `{ActionType} on game {GameId} was rejected with {ErrorCode}` | A command failed validation |
| 1003 | `ConcurrencyConflict` | Warning | `{ActionType} on game {GameId} failed with a concurrency conflict ({ConflictReason})` | A concurrency conflict occurred |
| 1010 | `GameCreated` | Information | `Game {GameId} created with {MaxPlayersCount} seats and a small blind of {SmallBlind}` | A game was created |
| 1011 | `HandStarted` | Information | `Hand {HandNo} of game {GameId} started with {SeatCount} seats` | A hand was started |
| 1012 | `HandFinished` | Information | `Hand {HandNo} of game {GameId} finished` | A hand was completed |
| 1013 | `GameFinished` | Information | `Game {GameId} finished` | A game was finished |
| 1020 | `StreetDealt` | Debug | `Hand {HandNo} of game {GameId} moved to {Street}` | The hand moved to a new street |
| 1100 | `HoleCardsDealt` | Trace | `Hand {HandNo} of game {GameId}: seat {SeatNo} was dealt {Cards}` | Hole cards were dealt (when `EnableSensitiveCardLogging` is on) |
| 1101 | `BoardDealt` | Trace | `Hand {HandNo} of game {GameId}: {Street} dealt {Cards}` | Community cards were dealt (when `EnableSensitiveCardLogging` is on) |

**Scope:** Every log record is stamped with the game ID and hand number through a logger scope opened at the start of each call:

```
GameId:{GameId} HandNo:{HandNo}
```

## Traces

The engine opens one `Activity` (a span in OpenTelemetry terminology) per call to `HoldemEngine.ApplyHoldemActionAsync`. On the first call of each `HoldemEngine` instance, a schema check runs before the span and the metrics: if the database is newer or needs migration, `HoldemSchemaException` is thrown before any signal is emitted.

**Source name:** `"OpenHoldemEngine"`

**Span name:** `"OpenHoldemEngine.ApplyHoldemAction"`

**Span kind:** `Internal`

**Tags:** Recorded when the tracer provider's `IsAllDataRequested` is true.

| Tag Name | Type | Values | When Set |
|---|---|---|---|
| `openholdemengine.action.type` | string | `CreateGame`, `GetGame`, `StartGame`, `StartHand`, `Play`, `FinishGame`, or `Unknown` | Always (at start) |
| `openholdemengine.game.id` | string | The game ID as a GUID string | Always (at start) |
| `openholdemengine.player_action.type` | string | `Fold`, `Check`, `Call`, `Raise`, `Bet`, `AllIn` | Only for `Play` actions |
| `openholdemengine.hand.no` | integer | The current hand's number | Set at return if hand number > 0 |
| `openholdemengine.street` | string | `PreFlop`, `Flop`, `Turn`, `River`, `Showdown`, `Finished` | Set at return if a hand is loaded |
| `openholdemengine.outcome` | string | `Success`, `Failure`, or `Exception` | Set at return |
| `openholdemengine.error.code` | string | The error code name (e.g., `InvalidSeat`, `Conflict`) | Set at return if outcome is `Failure` |

**Status:** The span status is set according to the outcome:

- **Unset** (the default): the command succeeded (`Success` outcome).
- **Error** with the exception type as description: an unhandled exception was thrown (`Exception` outcome).
- **Unset** on a domain failure: a `HoldemError` result (`Failure` outcome). Domain errors are not exceptions; they are normal results.

An exception is added to the span with `AddException()` when one is caught.

**EF instrumentation:** Entity Framework spans created by EF's own instrumentation nest under the engine span when EF instrumentation is enabled in the tracer provider.

## Metrics

The engine records four instruments. All metrics use tags to partition data; no metric tag has unbounded cardinality — every tag value is from a fixed enumeration or the list of defined error codes.

| Instrument Name | Type | Unit | Tags | What is Recorded |
|---|---|---|---|---|
| `openholdemengine.actions` | Counter (long) | `{action}` | `openholdemengine.action.type`, `openholdemengine.outcome` | Count of actions applied, one per call with the outcome (Success, Failure, Exception) recorded |
| `openholdemengine.hands` | Counter (long) | `{hand}` | (none) | Count of hands started |
| `openholdemengine.validation_failures` | Counter (long) | `{failure}` | `openholdemengine.action.type`, `openholdemengine.error.code` | Count of validation failures, one per error including conflicts |
| `openholdemengine.action.duration` | Histogram (double) | `s` | `openholdemengine.action.type`, `openholdemengine.outcome` | Duration of each action in seconds from start to end, exceptions included |

## Stability

These diagnostics names form the engine's public contract.

- Event ID numbers, log method names, and EventId values from `HoldemEngineLog` are fixed and stable.
- Span names, tag names, and tag value meanings from `HoldemEngineActivity` are fixed and stable.
- Instrument names, units, tag names, and tag value meanings are fixed and stable.
- The activity source name (`HoldemEngineTelemetry.ActivitySourceName`) and meter name (`HoldemEngineTelemetry.MeterName`) are fixed and stable.

**Breaking changes:** Renaming or removing a log event, a span tag, or an instrument, or changing an EventId number, a tag's meaning, or a unit is a breaking change equivalent to a CP diagnostic on `dotnet pack`. Removing a tag value or adding to unbounded cardinality is also a breaking change.

**Non-breaking changes:** Adding a new event, tag, or instrument is not a breaking change.

**Updating the document:** When a diagnostic is added, renamed or removed, this document must be updated in the same PR. The sync test `ObservabilityDocsTests` enforces that the document stays in step with the code.

## Sensitive data

Hole cards and the deck are never logged at Information level or above, and never appear in span tags or metric tags.

### The setting

Enable sensitive card logging with `EnableSensitiveCardLogging = true` in `HoldemEngineSettings`. By default it is `false`.

```csharp
services.AddHoldemEngine(dbOptions, configure: builder =>
    builder.WithSettings(settings => settings.EnableSensitiveCardLogging = true));
```

The setting is ignored unless the `OpenHoldemEngine` category is enabled at Trace level:

```csharp
services.AddLogging(logging => logging
    .SetMinimumLevel(LogLevel.Trace)
    .AddFilter("OpenHoldemEngine", LogLevel.Trace)
    .AddConsole());
```

### Card events

When enabled, the engine logs these events:

| Event ID | Name | Level | When |
|---|---|---|---|
| 1100 | `HoleCardsDealt` | Trace | Once per seat when a hand starts |
| 1101 | `BoardDealt` | Trace | Once per street (flop, turn, river) when board cards are dealt |

### Production safety

A host that mistakenly enables the setting but leaves the logger at Information or above will see no card events and pay no performance cost — the events are not written.

**Important:** The host's own EF `EnableSensitiveDataLogging()` prints card values in the `Microsoft.EntityFrameworkCore.Database.Command` category at Information level. That output is outside the engine's control. The test harness's `OHE_TEST_SQL_DEBUG` environment variable enables EF's sensitive data logging and therefore prints card values to the debug output.

## Current diagnostics (inventory before the milestone)

### Exceptions

The library throws 17 exceptions across 10 files. Each is one of three classes: a *caller error* (the host passed something invalid), a *broken invariant* (the engine's own state is inconsistent), or *infrastructure* (the database provider or another external system threw).

| File | Method | Exception | Trigger | Class |
|---|---|---|---|---|
| `Card.cs` | `Card` constructor | `ArgumentOutOfRangeException` | `cardType` is not a defined `CardType` | *caller error* |
| `Card.cs` | `Card` constructor | `ArgumentOutOfRangeException` | `cardSuit` is not a defined `CardSuit` | *caller error* |
| `Card.cs` | `Parse` | `FormatException` | `s` is not a two-character card token | *caller error* |
| `Data/EntityExtensions.cs` | `TryGetBlindSeats` | `ArgumentException` | A started hand has no small blind row | *broken invariant* |
| `Data/EntityExtensions.cs` | `TryGetBlindSeats` | `ArgumentException` | A started hand has no big blind row | *broken invariant* |
| `Engine/CardDeck.cs` | `Draw` | `InvalidOperationException` | `Draw()` called on an exhausted deck (e.g., `ICardShuffler` returns too few cards) | *caller error* |
| `Engine/HandRunner.cs` | `StartHand` | `InvalidOperationException` | Fewer than two funded seats are available to play | *broken invariant* |
| `Engine/HandRunner.cs` | `Accept` | `ArgumentOutOfRangeException` | An unexpected `PlayerActionType` is supplied | *broken invariant* |
| `Engine/HandRunner.cs` | `StartStreet` | `InvalidOperationException` | An unknown `Street` value is supplied (no next street) | *broken invariant* |
| `Engine/HandRunner.cs` | `EveryoneAgreed` | `InvalidOperationException` | No active seats remain (a state that should never happen) | *broken invariant* |
| `Engine/HandTable.cs` | Indexer `this[byte]` | `KeyNotFoundException` | The requested seat does not exist at the table | *broken invariant* |
| `Engine/StreetExtensions.cs` | `Next` | `InvalidOperationException` | `Next()` called on `Street.Finished` | *broken invariant* |
| `Engine/StreetExtensions.cs` | `BoardSize` | `NotSupportedException` | An unknown `Street` value is supplied | *broken invariant* |
| `HoldemActionHandlers/PlayActionHandler.cs` | `CalculateBetAmount` | `UnreachableException` | Default arm of a switch over defined `PlayerActionType` values (internal engine defect) | *broken invariant* |
| `HoldemActionHandlers/PlayActionHandler.cs` | `ToHandActionType` | `UnreachableException` | Default arm of a switch over defined `PlayerActionType` values (internal engine defect) | *broken invariant* |
| `HoldemResult.cs` | `Value` property | `InvalidOperationException` | `.Value` read on a failed result | *caller error* |
| `HoldemResult.cs` | `Fail` method | `ArgumentException` | `Fail()` called with no errors (an internal bug) | *broken invariant* |

### Exception handling

The library has one `catch`. It is `catch (DbUpdateConcurrencyException)` around `SaveChangesAsync` in `HoldemEngine.ApplyHoldemActionAsync`, and it turns the exception into `HoldemErrorCode.Conflict` (see [docs/task-run.md](task-run.md), fact 24). Every other exception reaches the caller unhandled.

### How exceptions will surface

The engine keeps not catching exceptions — it does not write them to its log either, because the host logs what reaches it. The host is responsible for deciding what to do with an unhandled exception (log it, report it, stop the request, etc.) and for logging the command's failure. Domain failures (`HoldemError`) are results, not exceptions, and are logged and counted as such by the host.
