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
| `MigrateDatabaseOnStartup` | `bool` | `false` | Migrates the database schema on start-up when `true` |

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

```csharp
builder.Services.AddHoldemEngine(
    builder.Configuration.GetSection("HoldemEngine"),
    db => db.UseHoldemSqlite(builder.Configuration.GetConnectionString("Holdem")!));
```

```json
{ "HoldemEngine": { "MigrateDatabaseOnStartup": true } }
```

When `MigrateDatabaseOnStartup` is set to `true` in the configuration, the engine registers a hosted
service that calls `IHoldemSchemaManager.MigrateAsync` during application start-up. The migration
completes before the ASP.NET Core server starts listening, so no request ever reaches an unmigrated
database.

**Default.** `false`, so a deployment must opt in explicitly. Migrating a production database is an
intentional decision.

**Failure behaviour.** A failed migration throws out of `StartAsync` and fails the host. There is no
fallback: a host must not serve against a schema it could not bring up to date.

**Multiple instances.** Rely on EF Core's migration lock. Since EF Core 9, `Migrate` takes a database
lock on every relational provider, so multiple instances starting concurrently will safely serialize
their migrations. No lock of our own is needed.

**Start-up ordering.** The hosted service is always registered, but does nothing when the flag is off.
It runs **before** any other hosted services you register after `AddHoldemEngine`, so the schema is
guaranteed to be up to date when other services start.

## Observability

To wire the engine's traces and metrics, add the instrumentation in one line:

```csharp
services
    .AddOpenTelemetry()
    .AddOpenHoldemEngineInstrumentation()
    .WithTracing(tracing => tracing.AddConsoleExporter())
    .WithMetrics(metrics => metrics.AddConsoleExporter());
```

The engine publishes logs through `ILogger<HoldemEngine>`. Add the `OpenHoldemEngine` category to
your logger configuration. Full details and alternative wiring options are in
[docs/observability.md](observability.md).

## REST API

Not implemented yet.

### Routes

Not implemented yet.

### JSON

Not implemented yet.

### Concurrency

Every successful response carries an `ETag: "<version>"` header where `<version>` is the game's current version.

An optional `If-Match` header on a write request specifies the version the client expects. If the header is present and does not match the loaded version, the write fails with status 412 Precondition Failed. If the header is absent, the write proceeds without a version check.

| If-Match value | Meaning | Conflict status |
|---|---|---|
| (absent) | No version check | 409 |
| `*` | No version check | 409 |
| `"7"` | Check that the version is 7 | 412 if version changed, 409 for other conflicts |
| Invalid value | 400 Bad Request | N/A |

**Valid and invalid If-Match values:**

Valid:
- (absent): the header is not sent
- `*`: wildcard, matches any version
- `"7"`: a strong entity tag with an integer version

Invalid (answered with 400 Bad Request):
- `W/"7"`: weak tag (RFC 9110 compares If-Match strongly)
- `7`: unquoted value
- `"7", "8"`: list of tags
- `"abc"`: non-integer tag value
- `"0"`: version zero (valid versions start at 1)
- `"-1"`: negative version
- `"99999999999"`: out of `int` range
- (whitespace only)

**POST /games and version checks.**

`CreateGame` has no version to compare: it creates a new game. The `If-Match` header is ignored on `POST /games`, but it still counts as present for the 412/409 rule of the `Conflict` code.

**Read operations.**

`GET` operations do not change the ETag and do not increment the version. The ETag of a reload matches the one read before, as long as no write changes the game.

### Errors

Every error returned by the engine comes back as an HTTP problem response with a `code` extension field containing the error code as a string.

| Code | Value | Status |
|---|---|---|
| `GameAlreadyExists` | 1 | 409 |
| `CurrentHandEnded` | 2 | 409 |
| `PreviousHandNotEnded` | 3 | 409 |
| `InvalidAction` | 4 | 422 |
| `UnsupportedGameOptions` | 5 | 422 |
| `InvalidGameOptions` | 6 | 422 |
| `InvalidSeat` | 7 | 422 |
| `GameAlreadyStarted` | 8 | 409 |
| `GameNotStarted` | 9 | 409 |
| `GameFinished` | 10 | 409 |
| `Conflict` | 11 | 412 when the request carried `If-Match`, 409 otherwise |
| `GameNotFound` | 12 | 404 |
| `IllegalAction` | 13 | 422 |

**The logic.**

- **422 Unprocessable Content**: The request was well-formed JSON, but the engine's rules reject its content. The game state is unchanged.
- **409 Conflict**: The request is valid, but the game's state does not allow it right now. Try again after reloading the game.
- **412 Precondition Failed**: Used for `Conflict` when the request carried an `If-Match` header. The game was modified; reload it and decide whether to retry.
- **404 Not Found**: The game ID does not exist. Valid only for actions other than `CreateGame`.
- **400 Bad Request**: Handled by the framework: malformed JSON, a binding failure, or a malformed `If-Match` value. Not mapped here.

**Example response body:**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.4.10",
  "title": "Conflict",
  "status": 409,
  "detail": "The game was changed by another action since you read it. Reload and decide.",
  "code": "Conflict"
}
```

**Exceptions and system errors.** `HoldemSchemaException` and other unhandled exceptions are not mapped here. They reach the host's exception handler, which by default answers 500 Internal Server Error with its own problem response. It is the host's choice how to wire exception handlers and problem details services.

### OpenAPI

Not implemented yet.

### Security

Not implemented yet.
