#### [TabletopCardEngine\.Sqlite](index.md 'index')

## TabletopCardEngine\.Sqlite Namespace

| Classes | |
| :--- | :--- |
| [HoldemSqliteDbContextOptionsBuilderExtensions](TabletopCardEngine.Sqlite.HoldemSqliteDbContextOptionsBuilderExtensions.md 'TabletopCardEngine\.Sqlite\.HoldemSqliteDbContextOptionsBuilderExtensions') | Configures the engine's database as SQLite with the engine's own migrations\. The plain `UseSqlite` call is not enough: EF would look for migrations in the core assembly, which has none, and [MigrateAsync\(CancellationToken\)](../TabletopCardEngine/TabletopCardEngine.Holdem.IHoldemSchemaManager.md#TabletopCardEngine.Holdem.IHoldemSchemaManager.MigrateAsync(System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemSchemaManager\.MigrateAsync\(System\.Threading\.CancellationToken\)') would fail with [MigrationsNotFound](../TabletopCardEngine/TabletopCardEngine.Holdem.HoldemSchemaFailure.md#TabletopCardEngine.Holdem.HoldemSchemaFailure.MigrationsNotFound 'TabletopCardEngine\.Holdem\.HoldemSchemaFailure\.MigrationsNotFound')\. |
