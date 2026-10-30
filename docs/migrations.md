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
2. **Stop every instance of the old version.** A rolling upgrade is not supported: the first instance of the new version that runs the migration stops every old instance from accessing that database. Old instances then fail with `HoldemSchemaException(DatabaseIsNewer)` on the next call. Once migrated, the database cannot be used by an older engine version.
3. **Deploy the new package.**
4. **Run the migration.** Call `MigrateAsync` or set `MigrateDatabaseOnStartup: true` as above.

The schema version is tracked in the database and compared on every engine call. A schema migrated by a newer engine cannot be downgraded: `GetSchemaStatusAsync` returns `NewerThanEngine`, and the engine refuses to run. If you need to go back to an older engine version, you must restore a backup of the database created before the upgrade.

For details on versioning and the schema version number, see [Versioning](versioning.md#the-database-schema-and-the-package-version).

### 1.0 to 1.1

The 1.0 to 1.1 migration is `AddGameTableFormat`. It adds the column `TableFormat` to the `Game` table with a default value of `SixMax`, the table format that implicitly existed before. The schema version is incremented from 1 to 2, and the engine version is updated to `1.1.0`.

When you upgrade from 1.0 to 1.1:

1. Back up your database.
2. Stop every instance of the 1.0 engine.
3. Deploy the 1.1 package.
4. Run `MigrateAsync` or restart your application (if `MigrateDatabaseOnStartup` is enabled).

No data is lost and no manual steps are required. Existing games are read back as `SixMax`. The migration is applied by `MigrateAsync` automatically when it runs.

## `EnsureCreated` is not supported

Do not use `EnsureCreated()` on a relational provider (SQLite, PostgreSQL, SQL Server). A database created that way has no migration history and cannot be upgraded: `MigrateAsync` will fail with `HoldemSchemaFailure.MigrationsNotFound`, and you will have to drop the database and start over. Use `MigrateAsync` instead.
