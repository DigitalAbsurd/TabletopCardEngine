# TabletopCardEngine.PostgreSql

PostgreSQL provider for `TabletopCardEngine`. Use this for production deployments.

```bash
dotnet add package TabletopCardEngine.PostgreSql --prerelease
```

Register the engine with PostgreSQL:

```csharp
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.PostgreSql;

var services = new ServiceCollection();
var connectionString = "Host=localhost;Database=holdem;Username=user;Password=pass";
services.AddHoldemEngine(db => db.UseHoldemPostgreSql(connectionString));
using var provider = services.BuildServiceProvider();

// Create or upgrade the database schema
await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
var engine = provider.GetRequiredService<IHoldemTable>();
```

All tables live in the `tce` schema. It is recommended to dedicate a database or at least a login to the engine.

**Documentation:** [Database providers](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/providers.md) · [Repository](https://github.com/DigitalAbsurd/TabletopCardEngine)
