# The ASP.NET Core package

## What this document is

The user guide of `OpenHoldemEngine.AspNetCore`, the package that wires the engine into an ASP.NET
Core host and exposes it over HTTP. The decisions behind it, the name, the dependency rule, the HTTP
contract and the security stance, are recorded in
[ADR-0004](adr/0004-aspnetcore-integration-package.md). Each section below is filled in by the issue
that implements it.

## Registration

```csharp
builder.Services.AddHoldemEngine(
    builder.Configuration.GetSection("HoldemEngine"),
    db => db.UseHoldemSqlite(builder.Configuration.GetConnectionString("Holdem")!));
```

```json
{ "ConnectionStrings": { "Holdem": "Data Source=holdem.db" },
  "HoldemEngine": { "GamePlayersMaxCount": 6 } }
```

The database stays in code: the package references no provider (ADR-0004), so the host passes the
`dbOptionsAction` that calls `UseHoldem*`. The section is bound to `HoldemEngineHostOptions` with the
Options pattern, validated with data annotations, and checked when the host starts.

| Key | Type | Default | Rule |
|---|---|---|---|
| `GamePlayersMinCount` | `int` | `2` | 2 to 6, and not above `GamePlayersMaxCount` |
| `GamePlayersMaxCount` | `int` | `6` | 2 to 6, and not below `GamePlayersMinCount` |
| `EnableSensitiveCardLogging` | `bool` | `false` | Logs hole cards and the board at Trace level; never for production |

A bad value fails `StartAsync` with an `OptionsValidationException` before any request, instead of
throwing `ArgumentOutOfRangeException` from service registration.

**Precedence.** The configuration section is the source of the settings, and it overwrites a
`WithSettings` call made inside the `configure` callback. `configure` remains the place for
`WithCardShuffler`.

## Health check

```csharp
builder.Services.AddHealthChecks().AddHoldemEngineCheck();

app.MapHealthChecks("/health");
```

`AddHoldemEngineCheck(name = "openholdemengine", failureStatus = null, tags = null)` registers a check
that calls `IHoldemSchemaManager.GetSchemaStatusAsync` on every probe. It reads `__OheSchema`, a
single-row query, and caches nothing. It never migrates the database: a health probe must not change
it, and migrating on start-up is a separate feature (below).

| State | Result | Description |
|---|---|---|
| `UpToDate` | `Healthy` | `Schema version {n} is up to date.` |
| `NotVersioned` | `Healthy` | `The provider keeps no schema version.` |
| `MigrationRequired` | the failure status | `The database schema needs migrating ({count} pending migrations).` |
| `NewerThanEngine` | the failure status | `The database was migrated by a newer engine (schema {db}, this engine expects {engine}).` |

The failure status is `Unhealthy` unless the host passes `failureStatus`. Neither failing state is
"degraded" by default: in both, the engine refuses every call.

The entry's `data` carries `state` (the `HoldemSchemaState` name), `providerName`,
`engineSchemaVersion`, `pendingMigrations` (a count), and `databaseSchemaVersion` and
`databaseEngineVersion` when the database records them. A key with no value is left out, never set to
null.

**Unreachable database.** The check catches no exception. When the provider throws (the server is
down, the file is not a database) or the provider is unsupported (`HoldemSchemaException`), the health
check service reports the failure status with the exception attached. One provider quirk: EF Core's
SQLite provider decides the database exists by looking for the file, without opening it, so a missing
SQLite file reports `MigrationRequired` rather than an exception. It is unhealthy all the same.

The default response writer of `MapHealthChecks` prints only the status (`Healthy`, `Degraded` or
`Unhealthy`). The sample shows a JSON writer that includes `data`.

## Migrating on start-up

Not implemented yet.

## Observability

Not implemented yet.

## REST API

Not implemented yet.

### Routes

Not implemented yet.

### JSON

Not implemented yet.

### Concurrency

Not implemented yet.

### Errors

Not implemented yet.

### OpenAPI

Not implemented yet.

### Security

Not implemented yet.
