#### [TabletopCardEngine\.SqlServer](index.md 'index')

## TabletopCardEngine\.SqlServer Namespace

| Classes | |
| :--- | :--- |
| [HoldemSqlServerDbContextOptionsBuilderExtensions](TabletopCardEngine.SqlServer.HoldemSqlServerDbContextOptionsBuilderExtensions.md 'TabletopCardEngine\.SqlServer\.HoldemSqlServerDbContextOptionsBuilderExtensions') | Configures the engine's database as SQL Server with the engine's own migrations in the `tce_holdem` schema\. The plain `UseSqlServer` call is not enough: EF would look for migrations in the core assembly, which has none, and [MigrateAsync\(CancellationToken\)](../TabletopCardEngine/TabletopCardEngine.Holdem.IHoldemSchemaManager.md#TabletopCardEngine.Holdem.IHoldemSchemaManager.MigrateAsync(System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemSchemaManager\.MigrateAsync\(System\.Threading\.CancellationToken\)') would fail with [MigrationsNotFound](../TabletopCardEngine/TabletopCardEngine.Holdem.HoldemSchemaFailure.md#TabletopCardEngine.Holdem.HoldemSchemaFailure.MigrationsNotFound 'TabletopCardEngine\.Holdem\.HoldemSchemaFailure\.MigrationsNotFound')\. |
