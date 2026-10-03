# Container images

## What this document is

The user guide for the OpenHoldemEngine container images. The decisions behind the host, the
container strategy, the provider model and the security stance are recorded in
[ADR-0005](adr/0005-container-host-and-images.md). Each section below is filled in by the issue
that implements it.

## Security

The API returns `GameData` exactly as the engine does, every seat's hole cards included. It is a
trusted, game-master API: the host is for a trusted network or for running behind a reverse proxy
that authenticates and controls access.

There is no API authentication in the container. Any caller that reaches the container can read
every card and place any action. Protect the container accordingly: restrict inbound access to
trusted hosts only, run it behind an authenticating reverse proxy, or both.

## Configuration

The container host is `src/OpenHoldemEngine.Server`, an ASP.NET Core application over the
`OpenHoldemEngine.AspNetCore` package. It reads its settings the usual ASP.NET Core way, each source
overriding the one before it:

1. `appsettings.json`, which carries logging and `AllowedHosts` only. It holds no connection string
   and no `HoldemEngine` section, so nothing database-specific is baked into an image.
2. `appsettings.Development.json`, read only when `ASPNETCORE_ENVIRONMENT` is `Development`. It
   points at a local SQLite file, `holdem.db`, and migrates it on start-up.
3. Environment variables, where `__` stands for the `:` of a configuration key
   (`HoldemEngine__MigrateDatabaseOnStartup` sets `HoldemEngine:MigrateDatabaseOnStartup`).

The settings it reads so far:

| Setting | Meaning |
|---|---|
| `ConnectionStrings__Holdem` | The connection string of the database, required. The host runs on SQLite for now, so this is a SQLite connection string such as `Data Source=/data/holdem.db`. |
| `HoldemEngine__*` | The engine's host options, bound to `HoldemEngineHostOptions`: `GamePlayersMinCount`, `GamePlayersMaxCount`, `EnableSensitiveCardLogging` and `MigrateDatabaseOnStartup`. The keys, defaults and rules are in [docs/aspnetcore.md](aspnetcore.md). |

To run the host locally, in the `Development` environment on port 5298:

```
dotnet run --project src/OpenHoldemEngine.Server
```

It serves the REST API under `/api/holdem` and the health report at `/health`.

## OpenAPI

Not implemented yet.

## Images

Not implemented yet.

## Testing the images

Not implemented yet.

## Running with compose

Not implemented yet.
