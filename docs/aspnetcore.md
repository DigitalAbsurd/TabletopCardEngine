# The ASP.NET Core package

## What this document is

The user guide of `OpenHoldemEngine.AspNetCore`, the package that wires the engine into an ASP.NET
Core host and exposes it over HTTP. The decisions behind it, the name, the dependency rule, the HTTP
contract and the security stance, are recorded in
[ADR-0004](adr/0004-aspnetcore-integration-package.md). Each section below is filled in by the issue
that implements it.

## Getting started

`samples/AspNetCoreSample` is a complete host: a short `Program.cs`, a SQLite database
file and no package reference beyond this repository's projects. Read it first, then copy what you need.

```csharp
builder.Services.AddHoldemEngine(
    builder.Configuration.GetSection("HoldemEngine"),
    db => db.UseHoldemSqlite(builder.Configuration.GetConnectionString("Holdem")!));
builder.Services.AddHealthChecks().AddHoldemEngineCheck();

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteHealthJsonAsync });
app.MapHoldemEngineApi();
```

Its `appsettings.json` points `ConnectionStrings:Holdem` at `holdem.db` and sets
`HoldemEngine:MigrateDatabaseOnStartup` to `true`, so the first start creates the schema.

1. Run it: `dotnet run --project samples/AspNetCoreSample --urls http://localhost:5299`.
2. `curl -s http://localhost:5299/health` reports the schema state, `"state":"UpToDate"`.
3. Play a game with `samples/AspNetCoreSample/AspNetCoreSample.http` (create, start, get, act, a stale
   `If-Match`, the next hand, finish), or with `curl`:
   `curl -s -i -X POST http://localhost:5299/api/holdem/games -H "Content-Type: application/json" -d "{}"`
   answers `201` with `ETag: "1"` and a `Location` header.
4. Stop the server and delete `samples/AspNetCoreSample/holdem.db` (git ignores it).

`SampleEndToEndTests` in `src/OpenHoldemEngine.AspNetCore.Tests` runs the same sample through
`WebApplicationFactory<Program>` on a temporary database file: a game from creation to finish, a stale
`If-Match` answered with 412 `Conflict`, and the health check in both the migrated and the unmigrated
state. The sample's `Program` is `internal` and reaches the tests through `InternalsVisibleTo`.

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

```csharp
app.MapHoldemEngineApi();                 // under /api/holdem
app.MapHoldemEngineApi("/poker");         // under a prefix of the host's choice
```

`MapHoldemEngineApi(prefix = "/api/holdem")` maps the engine's REST API as a minimal API route group and
returns the `RouteGroupBuilder`, so the host can chain `RequireAuthorization()`, rate limiting and the
like onto every route. Nothing is mapped unless the host calls it. It needs the registration of this
package, `services.AddHoldemEngine(configuration, ...)`, and throws `InvalidOperationException` without
it. The package adds no middleware: the pipeline (`UseAuthorization`, exception handling) is the host's.

Every route goes through one dispatcher: it reads `If-Match` (see [Concurrency](#concurrency)), calls the
engine with the request's abort token, answers a failure with the problem response of
[Errors](#errors), and on success sets the `ETag` header and returns the body of [JSON](#json).

### Routes

| Method | Path | Engine action | Success | Problem statuses |
|---|---|---|---|---|
| `POST` | `/games` | `CreateGame` | 201 with `GameData`, `Location` header, and an `ETag` | 400 (malformed JSON), 409 `GameAlreadyExists`, 422 `InvalidGameOptions` |
| `POST` | `/games/{gameId:guid}/start` | `StartGame` | 200 with `GameData` and an `ETag` | 400 (bad `If-Match`), 404 `GameNotFound`, 409 `GameAlreadyStarted`, 412 `Conflict`, 422 `InvalidSeat` |
| `GET` | `/games/{gameId:guid}` | `GetGame` | 200 with `GameData` and an `ETag` | 400 (bad `If-Match`), 404 `GameNotFound`, 412 `Conflict` |
| `POST` | `/games/{gameId:guid}/hands` | `StartHand` | 200 with `GameData` and an `ETag` | 400 (bad `If-Match`), 404 `GameNotFound`, 409 `GameNotStarted` \| `PreviousHandNotEnded`, 412 `Conflict` |
| `POST` | `/games/{gameId:guid}/actions` | `Play` | 200 with `GameData` and an `ETag` | 400 (bad `If-Match`, malformed JSON, numeric enum), 404 `GameNotFound`, 409 `GameFinished`, 412 `Conflict`, 422 `IllegalAction` |
| `POST` | `/games/{gameId:guid}/finish` | `FinishGame` | 200 with `GameData` and an `ETag` | 400 (bad `If-Match`), 404 `GameNotFound`, 409 `PreviousHandNotEnded`, 412 `Conflict` |

A `gameId` that is not a GUID does not match the route and answers 404.

**Examples.**

`POST /games` with an empty body creates a 6-seat game with 1/2 blinds:

```json
{}
```

`POST /games` with options and a game ID:

```json
{
  "gameId": "550e8400-e29b-41d4-a716-446655440000",
  "options": {
    "maxPlayersCount": 2,
    "initialSmallBlind": 0.5
  }
}
```

`POST /games/{gameId}/start` joins players to a game:

```json
{
  "joinSeats": [
    { "name": "Alice", "seatNo": 1, "buyInAmount": 100 },
    { "name": "Bob", "seatNo": 2, "buyInAmount": 100 }
  ]
}
```

`POST /games/{gameId}/hands` deals the next hand, optionally seating new players (body is optional):

```json
{
  "joinSeats": []
}
```

`POST /games/{gameId}/actions` plays one action for the seat whose turn it is. For `Raise` and `Bet`, `Size` is the street total to raise or bet *to*, and it is ignored for other actions:

```json
{
  "actionType": "Call"
}
```

or

```json
{
  "actionType": "Raise",
  "size": 42
}
```

`POST /games/{gameId}/finish` closes the game between hands (no body):

```json
{}
```

### JSON

The engine's enums and `Card` values have type-specific JSON converters registered in the host's
`Microsoft.AspNetCore.Http.Json.JsonOptions`, so no other type of the host is affected.

**Enum serialization.** Engine enums (`GameType`, `BettingStructure`, `CardType`, `CardSuit`,
`HandActionType`, `HandRankType`, `PlayerActionType`, `Street`) are serialized by name — e.g.
`"Holdem"`, `"NoLimit"`, `"Raise"` — not as numbers. Numbers are rejected on input, so an
undefined value cannot slip in as a number.

**Card serialization.** `Card` is serialized as a two-character string in the format rank-then-suit,
lowercase — e.g. `"ah"` (ace of hearts), `"tc"` (ten of clubs), `"2s"` (two of spades). Input is
case-insensitive and accepts either order — `"AH"`, `"HA"`, and `"ah"` all parse as the ace of
hearts. Output is always canonical.

**Example.** A heads-up game just after `StartGame`:

```json
{
  "gameId": "…",
  "options": {
    "gameType": "Holdem",
    "bettingStructure": "NoLimit",
    "maxPlayersCount": 2,
    "initialSmallBlind": 1,
    "initialDealerSeatNo": 1
  },
  "handData": {
    "no": 1,
    "seats": [
      {
        "seatNo": 1,
        "name": "a",
        "startHandStack": 100,
        "stack": 99,
        "betSize": 1,
        "hole": ["2d", "ks"],
        "lastAction": {
          "street": "PreFlop",
          "actionType": "SmallBlind",
          "size": 1,
          "no": 1
        }
      }
    ],
    "dealerSeatNo": 2,
    "smallBlindSeatNo": 1,
    "bigBlindSeatNo": 2,
    "nextPlayerSeatNo": 1,
    "street": "PreFlop",
    "streetPot": 3,
    "pot": 0,
    "communityCards": [],
    "evaluations": [
      {
        "playerActionType": "Call",
        "minValue": 1,
        "maxValue": 1
      }
    ]
  },
  "isFinished": false,
  "version": 2
}
```

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

Every endpoint is annotated with metadata for OpenAPI document generation: `WithName`, `WithTags`, `WithSummary`, `WithDescription`, and response type declarations (`Produces<GameData>`, `ProducesProblem`). The package ships no document generator — the host supplies its own with `Microsoft.AspNetCore.OpenApi` or a third-party package like Swashbuckle.

**With `Microsoft.AspNetCore.OpenApi`:**

```csharp
builder.Services.AddOpenApi();
app.MapOpenApi();
app.MapHoldemEngineApi();
```

The generated document will include the engine's endpoints under the tag `OpenHoldemEngine`.

**With Swashbuckle:**

```csharp
builder.Services.AddSwaggerGen();
app.UseSwagger();
app.UseSwaggerUI();
app.MapHoldemEngineApi();
```

**Grouping the API in its own document.** Call `WithGroupName` on the returned route group to put the API in a separate document:

```csharp
app.MapHoldemEngineApi().WithGroupName("holdem");
```

Both `Microsoft.AspNetCore.OpenApi` and Swashbuckle will then generate a separate document for the engine endpoints.

### Security

The API returns every seat's hole cards, as `GameData` does (ADR-0004). It is a trusted, game-master
API: protect the group, for example with
`app.MapHoldemEngineApi().RequireAuthorization("holdem-admin")`. Hiding cards per viewer is not in v1.
