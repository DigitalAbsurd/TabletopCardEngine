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

Not implemented yet.

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
