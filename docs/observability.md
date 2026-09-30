---
---

# Observability

This document describes the engine's diagnostics: what logging, tracing and metrics it produces, the contract their names form, and how it surfaces errors and failures to the host.

## Current diagnostics (inventory before the milestone)

### Logging

There is no logging, tracing or metrics code in `src/` at all. `git grep -nE "ILogger|LoggerMessage|ActivitySource|System.Diagnostics.Metrics|DiagnosticSource|OpenTelemetry" -- src` prints nothing. The only `using System.Diagnostics;` in the library is `HoldemActionHandlers/PlayActionHandler.cs`, and it is there for `UnreachableException`.

### Test-only diagnostic output

The only diagnostic output is test-only. `src/OpenHoldemEngine.Tests/TestEngineHost.cs` (the `#if DEBUG` block in `Setup`) calls `opts.LogTo(msg => System.Diagnostics.Debug.WriteLine(msg), minimumLevel: LogLevel.Information)` and `opts.EnableSensitiveDataLogging()` when `OHE_TEST_SQL_DEBUG=true`. EF therefore prints SQL with parameter values, which includes card strings from `HandHoleCard`/`HandDeckCard` inserts, to the debug output. This is also the only place in the repository that turns sensitive data logging on. **The `OHE_TEST_SQL_DEBUG` environment variable enables `EnableSensitiveDataLogging` and therefore prints card values.**

### Direct dependencies

Logging reaches the library only transitively. Its one direct package is `Microsoft.EntityFrameworkCore` 9.0.1. That brings `Microsoft.Extensions.Logging` 9.0.1, `Microsoft.Extensions.Logging.Abstractions` 9.0.1 and `System.Diagnostics.DiagnosticSource` 9.0.1 (see `src/OpenHoldemEngine/obj/project.assets.json` after a restore).

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

A future instrumentation feature (the tracing issue of this milestone) will record an exception on the command's span (status `Error`) before it propagates.
