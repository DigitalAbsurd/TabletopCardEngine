# Migrations

How to create and upgrade the database schema for the engine.

## Creating the schema

At application start-up, obtain the schema manager from the container and call `MigrateAsync` once, before the first engine call:

```csharp
var provider = services.BuildServiceProvider();
await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
```

This applies all pending migrations to the database. If the database does not exist, migrations create it. If it already exists and is up to date, `MigrateAsync` does nothing.

In an ASP.NET Core host, use the `MigrateDatabaseOnStartup` option instead:

```csharp
builder.Services.AddTabletopCardEngine(tce => tce.AddHoldem(
    builder.Configuration.GetSection("TabletopCardEngine:Holdem"),
    db => db.UseHoldemSqlite(builder.Configuration.GetConnectionString("TabletopCardEngine")!)));
```

```json
{ "TabletopCardEngine": { "Holdem": { "MigrateDatabaseOnStartup": true } } }
```

The host registers a hosted service that runs the migration during application start-up, before the server starts listening. See [ASP.NET Core integration](aspnetcore.md#migrating-on-start-up) for details, configuration options and failure handling.

The container images (SQLite, PostgreSQL, and SQL Server targets) do this by default.

## Where the tables are

Hold'em keeps its tables apart from the host's and from another game's:

| Provider | Tables | Migration history |
|---|---|---|
| PostgreSQL, SQL Server | in the schema `tce_holdem` | `tce_holdem.__TceMigrationsHistory` |
| SQLite | named with the prefix `tce_holdem_` | `tce_holdem___TceMigrationsHistory` |
| InMemory | no schema, no prefix | none |

Each provider package ships its migrations under `Migrations/Holdem/`. See [Persistence](persistence.md#where-the-engines-tables-live) for the details.

## Checking the schema without changing it

Call `GetSchemaStatusAsync` to inspect the schema's state without making any changes:

```csharp
var status = await provider.GetRequiredService<IHoldemSchemaManager>().GetSchemaStatusAsync();
```

This returns a `HoldemSchemaStatus` with one of four `HoldemSchemaState` values:

| State | Meaning | Engine can run |
|---|---|---|
| `UpToDate` | All migrations have been applied. | Yes |
| `MigrationRequired` | The schema is older than the engine or does not exist. | No, call `MigrateAsync` first |
| `NewerThanEngine` | The schema was migrated by a newer version of the engine and cannot be downgraded. | No, upgrade the engine |
| `NotVersioned` | The provider keeps no schema history (InMemory only). | Yes |

The health check `AddHoldemCheck()` reports the same state. See [ASP.NET Core integration](aspnetcore.md#health-check) for details.

## If you don't migrate before the first call

The engine checks the schema on its first call and throws `HoldemSchemaException` with one of two failure codes:

- `HoldemSchemaFailure.MigrationRequired` — the schema is older than the engine or does not exist.
- `HoldemSchemaFailure.DatabaseIsNewer` — the schema was migrated by a newer engine.

Neither state is recoverable at runtime: the engine refuses every call until the schema is corrected. Migrate before the first call instead.

## Upgrading the package

When you upgrade the `TabletopCardEngine` package to a new version:

1. **Back up the database.** A migration failure or a backward-incompatibility (rare but possible) could lose data.
2. **Deploy the new package.**
3. **Run the migration.** Call `MigrateAsync` or set `MigrateDatabaseOnStartup: true` as above.

The schema version is tracked in the database and compared on every engine call. A schema migrated by a newer engine cannot be downgraded: `GetSchemaStatusAsync` returns `NewerThanEngine`, and the engine refuses to run. If you need to go back to an older engine version, you must restore a backup of the database created before the upgrade.

For details on versioning and the schema version number, see [Versioning](versioning.md#the-database-schema-and-the-package-version).

## `EnsureCreated` is not supported

Do not use `EnsureCreated()` on a relational provider (SQLite, PostgreSQL, SQL Server). A database created that way has no migration history and cannot be upgraded: `MigrateAsync` will fail with `HoldemSchemaFailure.MigrationsNotFound`, and you will have to drop the database and start over. Use `MigrateAsync` instead.
