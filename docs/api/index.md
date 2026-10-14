# API reference

These pages document the public .NET surface of the seven packages below. They are generated from XML doc comments by `scripts/generate-api-reference.ps1` ([ADR-0009](../adr/0009-generated-api-reference.md)) and are not edited by hand.

| Package | What it is | Reference |
|---------|-----------|-----------|
| TabletopCardEngine | No-Limit Texas Hold'em cash-game engine for .NET: betting rules, blinds, side pots and showdown, persisted through Entity Framework Core and driven by a single async command method. | [TabletopCardEngine](TabletopCardEngine/index.md) |
| TabletopCardEngine.Sqlite | SQLite provider for TabletopCardEngine, with the engine's schema migrations, for development, prototypes and small deployments. | [TabletopCardEngine.Sqlite](TabletopCardEngine.Sqlite/index.md) |
| TabletopCardEngine.PostgreSql | PostgreSQL provider for TabletopCardEngine, with the engine's schema migrations for Npgsql. | [TabletopCardEngine.PostgreSql](TabletopCardEngine.PostgreSql/index.md) |
| TabletopCardEngine.SqlServer | SQL Server provider for TabletopCardEngine, with the engine's schema migrations for Microsoft SQL Server. | [TabletopCardEngine.SqlServer](TabletopCardEngine.SqlServer/index.md) |
| TabletopCardEngine.InMemory | Entity Framework Core in-memory provider for TabletopCardEngine, for tests and prototypes rather than production. | [TabletopCardEngine.InMemory](TabletopCardEngine.InMemory/index.md) |
| TabletopCardEngine.AspNetCore | ASP.NET Core integration for TabletopCardEngine: registers the engine in a host and exposes it over HTTP with schema health checks and a REST API. | [TabletopCardEngine.AspNetCore](TabletopCardEngine.AspNetCore/index.md) |
| TabletopCardEngine.OpenTelemetry | OpenTelemetry instrumentation for TabletopCardEngine: one call adds the engine's traces and metrics to a tracer or meter provider. | [TabletopCardEngine.OpenTelemetry](TabletopCardEngine.OpenTelemetry/index.md) |

## Where to start

- `IHoldemTable` — the one public method, `ApplyAsync(action, cancellationToken)`: [reference](TabletopCardEngine/TabletopCardEngine.Holdem.IHoldemTable.md)
- `HoldemAction` — the commands and their factory methods: [reference](TabletopCardEngine/TabletopCardEngine.Holdem.Dto.HoldemAction.md)
- `GameData` — what every call returns: [reference](TabletopCardEngine/TabletopCardEngine.Holdem.Dto.GameData.md)
- `HoldemErrorCode` — the result codes: [reference](TabletopCardEngine/TabletopCardEngine.Holdem.HoldemErrorCode.md)
- `TabletopCardEngineServiceCollectionExtensions` and `HoldemBuilderExtensions` — registration: [service collection](TabletopCardEngine/TabletopCardEngine.TabletopCardEngineServiceCollectionExtensions.md), [builder](TabletopCardEngine/TabletopCardEngine.Holdem.HoldemBuilderExtensions.md)

## Not covered here

- The REST API is in [ASP.NET Core](../aspnetcore.md) and the server's `/openapi/v1.json` ([Container images](../docker.md)).
- The compatibility rules (what may change in a minor release) are in [Public API](../public-api.md).
