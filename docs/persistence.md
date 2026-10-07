# Persistence: migrations and schema versioning

## What this document is

The engine's storage contract: which databases are supported, how the schema is created and versioned, and the rules for writing a migration. This document is for maintainers and for consumers. Consumers new to the engine should start with [Database providers](providers.md) and [Migrations](migrations.md), which cover choosing a provider and managing the schema; this document covers the maintainer's rules for writing a migration and the internal structure of the schema version row.

## Supported databases

| Database | Package | Registration call | Migrations |
|---|---|---|---|
| SQLite | `TabletopCardEngine.Sqlite` | `UseHoldemSqlite(...)`, [setup](#sqlite) | yes |
| PostgreSQL | `TabletopCardEngine.PostgreSql` | `UseHoldemPostgreSql(...)` | yes |
| SQL Server | `TabletopCardEngine.SqlServer` | `UseHoldemSqlServer(...)` | yes |
| EF Core InMemory | `TabletopCardEngine.InMemory` | `UseHoldemInMemory(databaseName)` | no, created on `MigrateAsync` |

Any other EF Core provider is rejected by `IHoldemSchemaManager` with `HoldemSchemaException(UnsupportedProvider)`. The core `TabletopCardEngine` package references no database provider, so a SQLite consumer never restores `Npgsql` or `Microsoft.Data.SqlClient`.

### SQLite

Reference `TabletopCardEngine.Sqlite`, then register and migrate:

```csharp
services.AddHoldemEngine(o => o.UseHoldemSqlite("Data Source=holdem.db"));
await services.BuildServiceProvider().GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
```

For an in-memory database, pass an open `SqliteConnection("DataSource=:memory:")` instead of the connection string, and keep it open for as long as the engine runs. The plain `UseSqlite` call is not enough: it points EF at no migrations, and `MigrateAsync` fails with `MigrationsNotFound`.

## Creating and upgrading the schema

Register the engine with a database provider:

```csharp
services.AddHoldemEngine(o => o.UseHoldemSqlite(connectionString))
```

At application start-up, obtain the schema manager and call `MigrateAsync`:

```csharp
var provider = services.BuildServiceProvider();
await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
```

### Schema status

`GetSchemaStatusAsync` reports one of four `HoldemSchemaState` values:

- `UpToDate` (1) — every migration the engine knows of has been applied, and the engine may run.
- `MigrationRequired` (2) — the schema is older than the engine or does not exist, and must be upgraded before the engine runs.
- `NewerThanEngine` (3) — the schema was migrated by a newer engine, and cannot be downgraded. A newer engine is required.
- `NotVersioned` (4) — the provider keeps no schema history (InMemory), so there is no version to compare; the engine may run.

An unsupported provider is not a state: `GetSchemaStatusAsync` throws
`HoldemSchemaException(UnsupportedProvider)` for it. A database that cannot be reached is not a state
either: the provider's own exception surfaces unwrapped.

### Runtime schema checks

Each `HoldemEngine` checks the schema version on its first call. A schema newer than the engine throws `HoldemSchemaException(DatabaseIsNewer)`. A schema older or missing throws `HoldemSchemaException(MigrationRequired)`.

`EnsureCreated` is not supported for a relational database on a relational provider, because a database created that way has no migration history and can never be upgraded. Use `MigrateAsync` instead.

## Where the engine's tables live

On PostgreSQL and SQL Server, every table is in the schema `tce`. SQLite has no schemas.

The migration history is the table `__TceMigrationsHistory`, not EF's default `__EFMigrationsHistory`, so it never collides with a host's own EF context running over the same database.

The schema version is the single row of `__TceSchema` with `Id = 1`, holding `SchemaVersion` and `EngineVersion`. This row is model seed data (`HasData`), and so each migration writes it.

It is recommended to dedicate a database, or at least a login, to the engine.

## Schema versioning

`HoldemSchema.Version` is an integer, bumped by one in the same change as every migration. `HoldemSchema.EngineVersion` is the release that introduced the schema version.

A migration requires at least a minor release, never a patch, and `EngineVersion` is that release's version without a pre-release suffix. See [docs/versioning.md#the-database-schema-and-the-package-version](versioning.md#the-database-schema-and-the-package-version).

The schema version row is model seed data (`HasData`), so each migration writes it.

### Start-up rule

The start-up rule is a three-state lookup:

| Database version | Engine version | Action |
|---|---|---|
| Greater than the engine's | — | Refused with `DatabaseIsNewer` |
| Lower, or no row exists | — | Migrate first with `MigrationRequired` |
| Equal | — | Runs |

There is no downgrade: an older engine never runs on a newer database.

## Writing a migration

Migrations are written per the following rules:

1. **Add migrations only with `pwsh ./scripts/add-migration.ps1 -Name AddSomething`.** Never run `dotnet ef migrations add` for one provider alone. The script generates the migration for every supported provider at once.

2. **Bump `HoldemSchema.Version` in the same change as every migration.** Also bump `EngineVersion` when the release changes. Make the changes to `HoldemSchema` itself and to the seed data (`HasData`) in each provider's migration in the same commit. A migration requires at least a minor release; see [docs/versioning.md#the-database-schema-and-the-package-version](versioning.md#the-database-schema-and-the-package-version).

3. **Renames use `migrationBuilder.RenameColumn` / `RenameTable`.** The scaffolder often writes a drop and add instead. Review every generated `Up` method and replace drop + add with a rename. A drop and add on a populated table loses the column's data.

4. **Key changes and data backfills are written by hand per provider**, as `migrationBuilder.Sql(...)` inside `if (migrationBuilder.ActiveProvider == "...")` blocks, one block per supported provider. A missing block for one provider is a defect. Include a short example:

   ```csharp
   if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
   {
       migrationBuilder.Sql(@"
           UPDATE tce.player_seats
           SET name = TRIM(name)
       ");
   }
   else if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
   {
       migrationBuilder.Sql(@"
           UPDATE [tce].[player_seats]
           SET [name] = TRIM([name])
       ");
   }
   else if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
   {
       migrationBuilder.Sql(@"
           UPDATE player_seats
           SET name = TRIM(name)
       ");
   }
   ```

5. **Every migration after `InitialCreate` ships with an upgrade fixture on seeded data.** Implement `IUpgradeFixture` in `tests/TabletopCardEngine.Tests/Persistence/Upgrades/`, with `MigrationName` set to the migration's name. `SeedAsync` writes rows with raw SQL valid at the previous migration, per provider: neither the engine nor `HoldemDbContext` speaks the previous schema. `AssertAsync` then checks the rows through a fully migrated engine. `ProviderSchemaTests.Upgrade_EveryMigrationKeepsItsSeededData` runs each fixture on every relational database, and the guard `Upgrade_EveryMigrationAfterTheFirstHasAFixture` fails, naming the migration and the provider, when a migration has no fixture or more than one. The upgrade tests run on SQLite in every `dotnet test`, and on PostgreSQL and SQL Server in Docker under `TCE_TEST_CONTAINERS=1`: locally when Docker is available, and always in CI's `providers` job, on `main` and on every pull request that touches `Data/`, a provider package or the migrations' tests.

6. **Generated migration and snapshot files are `internal` and are not edited**, except for the hand-written parts rules 3 and 4 require.

7. **A released migration is never edited or deleted.** Fix it forward with a new migration.

## The InMemory provider

Reference `TabletopCardEngine.InMemory`, then register and migrate:

```csharp
services.AddHoldemEngine(o => o.UseHoldemInMemory("my-game-db"));
await services.BuildServiceProvider().GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
```

The `MigrateAsync` call is optional for InMemory; on the first engine action the store is created automatically.

### Limits

The InMemory provider is for prototyping, demos and consumers' unit tests, not for production:

- **State lives only in the process and is lost on restart.** A reload discards everything; use `.phh` export to keep a hand.
- **Foreign keys are not enforced.** The database will not catch referential integrity errors.
- **There are no transactions.** EF ignores calls to `BeginTransaction`. The engine does not use explicit transactions, so this does not change what a call does.
- **A lost race is detected before the save, not inside it.** InMemory applies a save's inserts before its updates, so the `Game.Version` token would only be compared after a racing writer's rows had hit a duplicate key. On a non-relational provider the engine therefore re-reads `Game.Version` just before saving and returns `Conflict` when it moved. That narrows the window between that read and the save, but cannot close it the way a relational transaction does.
- **`decimal` precision is not applied,** although the engine's own validation rule (at most two decimal places) is still enforced by the engine itself.

`GetSchemaStatusAsync` reports `NotVersioned` on InMemory, because the provider has no versioning. Two engines given the same `databaseName` in one process will share the same in-memory store; use a unique name for each isolated engine (one per test, one per tenant).
