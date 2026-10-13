# TabletopCardEngine.Sqlite

SQLite provider for `TabletopCardEngine`. Use this for development, prototypes, and small deployments.

```bash
dotnet add package TabletopCardEngine.Sqlite --prerelease
```

Register the engine with SQLite and create or migrate the schema at startup:

```csharp
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Sqlite;

var services = new ServiceCollection();
services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemSqlite("Data Source=holdem.db")));
using var provider = services.BuildServiceProvider();

// Create or upgrade the database schema once, at startup
var schemaManager = provider.GetRequiredService<IHoldemSchemaManager>();
await schemaManager.MigrateAsync();

// The engine is now ready to use
var engine = provider.GetRequiredService<IHoldemTable>();
```

The `holdem.db` file holds the schema and all game data. If you restart the application, it reuses the same database. SQLite has no schemas, so every Hold'em table name starts with `tce_holdem_` (`tce_holdem_Game`, `tce_holdem_Hand`, ...).

**Documentation:** [Database providers](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/providers.md) · [Repository](https://github.com/DigitalAbsurd/TabletopCardEngine)
