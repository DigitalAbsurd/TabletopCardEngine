# Database providers

The engine supports four database providers through EF Core, each in its own package. Choose based on your deployment and requirements.

## Supported providers

| Provider | Package | Registration call | Connection string example | Migrations |
|---|---|---|---|---|
| SQLite | `TabletopCardEngine.Sqlite` | `UseHoldemSqlite(string)` or `UseHoldemSqlite(DbConnection)` | `Data Source=holdem.db` | yes |
| PostgreSQL | `TabletopCardEngine.PostgreSql` | `UseHoldemPostgreSql(string)` or `UseHoldemPostgreSql(DbDataSource)` | `Host=localhost;Port=5432;Database=holdem;Username=user;Password=pass` | yes |
| SQL Server | `TabletopCardEngine.SqlServer` | `UseHoldemSqlServer(string)` or `UseHoldemSqlServer(DbConnection)` | `Server=.;Database=holdem;Trusted_Connection=True;TrustServerCertificate=True` | yes |
| EF Core InMemory | `TabletopCardEngine.InMemory` | `UseHoldemInMemory(string databaseName)` | N/A (database name only) | created on `MigrateAsync` |

Any other EF Core provider is rejected by `IHoldemSchemaManager` with `HoldemSchemaException(UnsupportedProvider)`. The core `TabletopCardEngine` package references no database provider, so a consumer of any one provider never restores packages from the others.

## SQLite

SQLite is suitable for development, prototypes, and small deployments. Use `TabletopCardEngine.Sqlite` and call either overload:

```csharp
// File-based database
services.AddTabletopCardEngine(tce => tce.AddHoldem(o => o.UseHoldemSqlite("Data Source=holdem.db")));

// In-memory database (for tests or short-lived applications)
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
services.AddTabletopCardEngine(tce => tce.AddHoldem(o => o.UseHoldemSqlite(connection)));
// Keep connection open for the lifetime of the engine.
```

### What bites with SQLite

**In-memory databases require an open connection.** A plain `UseSqlite(":memory:")` call alone is not enough: EF Core's SQLite provider looks for migrations in the core assembly, which has none, and `MigrateAsync` fails with `HoldemSchemaFailure.MigrationsNotFound`. Instead, pass a `DbConnection` you hold open:

```csharp
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
services.AddTabletopCardEngine(tce => tce.AddHoldem(o => o.UseHoldemSqlite(connection)));
// The application must keep this connection open for the engine's lifetime.
// Closing it closes the database and discards all rows.
```

## PostgreSQL

PostgreSQL is suitable for production deployments. Use `TabletopCardEngine.PostgreSql` and call either overload:

```csharp
// Connection string (single connection, no pooling)
services.AddTabletopCardEngine(tce => tce.AddHoldem(o => o.UseHoldemPostgreSql("Host=localhost;Database=holdem;Username=user;Password=pass")));

// Data source (recommended for production, allows connection pooling)
var dataSource = new NpgsqlDataSourceBuilder("Host=localhost;Database=holdem;Username=user;Password=pass").Build();
services.AddTabletopCardEngine(tce => tce.AddHoldem(o => o.UseHoldemPostgreSql(dataSource)));
```

### What bites with PostgreSQL

**Namespace.** Add `using TabletopCardEngine.PostgreSql;` to your code.

**Schema.** All tables live in the schema `tce`, and the migration history table is `tce.__TceMigrationsHistory`. It is recommended to dedicate a database or at least a login to the engine.

**Licences.** The Npgsql package carries the PostgreSQL License. See [ADR-0003](adr/0003-database-provider-licences.md) for the licence terms and the decision to accept it.

## SQL Server

SQL Server is suitable for production deployments. Use `TabletopCardEngine.SqlServer` and call either overload:

```csharp
// Connection string (single connection, no pooling)
services.AddTabletopCardEngine(tce => tce.AddHoldem(o => o.UseHoldemSqlServer("Server=.;Database=holdem;Trusted_Connection=True;TrustServerCertificate=True")));

// Connection (for advanced connection management)
var connection = new SqlConnection("Server=.;Database=holdem;Trusted_Connection=True;TrustServerCertificate=True");
services.AddTabletopCardEngine(tce => tce.AddHoldem(o => o.UseHoldemSqlServer(connection)));
```

### What bites with SQL Server

**Namespace.** Add `using TabletopCardEngine.SqlServer;` to your code.

**Schema.** All tables live in the schema `tce`, and the migration history table is `tce.__TceMigrationsHistory`. It is recommended to dedicate a database or at least a login to the engine.

**Self-signed certificates require `TrustServerCertificate=True`.** The `Microsoft.Data.SqlClient` library encrypts connections by default. Most container SQL Server images use self-signed certificates, which fail validation without `TrustServerCertificate=True` in the connection string. If your SQL Server uses a proper certificate, you can omit this flag. Never disable SSL altogether; the flag only skips certificate validation.

**Licences.** The `Microsoft.Data.SqlClient.SNI.runtime` package is a native Windows component distributed under a Microsoft proprietary licence. See [ADR-0003](adr/0003-database-provider-licences.md) for the licence terms and the decision to grant a single-package exception.

## EF Core InMemory

InMemory is suitable for prototyping, testing, and isolated unit testing, **not for production**. Use `TabletopCardEngine.InMemory`:

```csharp
services.AddTabletopCardEngine(tce => tce.AddHoldem(o => o.UseHoldemInMemory("my-store")));
```

The string is a unique name for this in-memory store. Two engines given the same name will share the same data; use a unique name for each isolated engine (one per test, one per tenant, etc.).

### What bites with InMemory

InMemory has four limits:

- **State lives only in memory.** The process holds everything; restarting loses all data. There is no persistence and no recovery.
- **No schema versioning.** `GetSchemaStatusAsync` returns `HoldemSchemaState.NotVersioned`, so upgrading the package cannot be detected: the host cannot distinguish a forward or backward migration. Use only for tests and prototypes.
- **Foreign keys are not enforced.** Constraint violations are not caught; you must rely on the host's logic.
- **Transactions are ignored.** All writes appear committed immediately, and concurrency conflicts cannot be detected through the normal channels.

One advantage: InMemory does not need Docker or any external database, so it is fast for testing.
