# Persistence: migrations and schema versioning

## What this document is

The engine's storage contract: which databases are supported, how the schema is created and versioned, and the rules for writing a migration. This document is for maintainers and for consumers.

## Supported databases

| Database | Package | Registration call | Migrations |
|---|---|---|---|
| SQLite | `OpenHoldemEngine.Sqlite` | `UseHoldemSqlite(...)`, [setup](#sqlite) | yes |
| PostgreSQL | `OpenHoldemEngine.PostgreSql` | `UseHoldemPostgreSql(...)` | yes |
| SQL Server | `OpenHoldemEngine.SqlServer` | `UseHoldemSqlServer(...)` | yes |
| EF Core InMemory | `OpenHoldemEngine.InMemory` | `UseHoldemInMemory(databaseName)` | no, created on `MigrateAsync` |

Any other EF Core provider is rejected by `IHoldemSchemaManager` with `HoldemSchemaException(UnsupportedProvider)`. The core `OpenHoldemEngine` package references no database provider, so a SQLite consumer never restores `Npgsql` or `Microsoft.Data.SqlClient`.

### SQLite

Reference `OpenHoldemEngine.Sqlite`, then register and migrate:

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

- `Ready` — the schema version matches the engine version, and the engine may run.
- `MigrationRequired` — the schema is older than the engine or does not exist, and must be upgraded before the engine runs.
- `DatabaseIsNewer` — the schema is newer than the engine, and cannot be downgraded. A newer engine is required.
- `UnsupportedProvider` — the database provider is not supported by `IHoldemSchemaManager`.

### Runtime schema checks

Each `HoldemEngine` checks the schema version on its first call. A schema newer than the engine throws `HoldemSchemaException(DatabaseIsNewer)`. A schema older or missing throws `HoldemSchemaException(MigrationRequired)`.

`EnsureCreated` is not supported for a relational database on a relational provider, because a database created that way has no migration history and can never be upgraded. Use `MigrateAsync` instead.

## Where the engine's tables live

On PostgreSQL and SQL Server, every table is in the schema `ohe`. SQLite has no schemas.

The migration history is the table `__OheMigrationsHistory`, not EF's default `__EFMigrationsHistory`, so it never collides with a host's own EF context running over the same database.

The schema version is the single row of `__OheSchema` with `Id = 1`, holding `SchemaVersion` and `EngineVersion`. This row is model seed data (`HasData`), and so each migration writes it.

It is recommended to dedicate a database, or at least a login, to the engine.

## Schema versioning

`HoldemSchema.Version` is an integer, bumped by one in the same change as every migration. `HoldemSchema.EngineVersion` is the release that introduced the schema version.

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

2. **Bump `HoldemSchema.Version` in the same change as every migration.** Also bump `EngineVersion` when the release changes. Make the changes to `HoldemSchema` itself and to the seed data (`HasData`) in each provider's migration in the same commit.

3. **Renames use `migrationBuilder.RenameColumn` / `RenameTable`.** The scaffolder often writes a drop and add instead. Review every generated `Up` method and replace drop + add with a rename. A drop and add on a populated table loses the column's data.

4. **Key changes and data backfills are written by hand per provider**, as `migrationBuilder.Sql(...)` inside `if (migrationBuilder.ActiveProvider == "...")` blocks, one block per supported provider. A missing block for one provider is a defect. Include a short example:

   ```csharp
   if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
   {
       migrationBuilder.Sql(@"
           UPDATE ohe.player_seats
           SET name = TRIM(name)
       ");
   }
   else if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
   {
       migrationBuilder.Sql(@"
           UPDATE [ohe].[player_seats]
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

5. **Every migration that changes a key or backfills data ships with an upgrade test on seeded data.** Seed the previous schema, run the migration, and assert the rows survived. Upgrade tests run on every supported database. The test matrix enforces this and fails when a migration has no upgrade fixture.

6. **Generated migration and snapshot files are `internal` and are not edited**, except for the hand-written parts rules 3 and 4 require.

7. **A released migration is never edited or deleted.** Fix it forward with a new migration.

## The InMemory provider

Reference `OpenHoldemEngine.InMemory`, then register and migrate:

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
- **`decimal` precision is not applied,** although the engine's own validation rule (at most two decimal places) is still enforced by the engine itself.

`GetSchemaStatusAsync` reports `NotVersioned` on InMemory, because the provider has no versioning. Two engines given the same `databaseName` in one process will share the same in-memory store; use a unique name for each isolated engine (one per test, one per tenant).
