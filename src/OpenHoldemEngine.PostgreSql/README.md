# OpenHoldemEngine.PostgreSql

PostgreSQL provider for `OpenHoldemEngine`. Use this for production deployments.

```bash
dotnet add package OpenHoldemEngine.PostgreSql --prerelease
```

Register the engine with PostgreSQL:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine;
using OpenHoldemEngine.PostgreSql;

var services = new ServiceCollection();
var connectionString = "Host=localhost;Database=holdem;Username=user;Password=pass";
services.AddHoldemEngine(db => db.UseHoldemPostgreSql(connectionString));
using var provider = services.BuildServiceProvider();

// Create or upgrade the database schema
await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
var engine = provider.GetRequiredService<HoldemEngine>();
```

All tables live in the `ohe` schema. It is recommended to dedicate a database or at least a login to the engine.

**Documentation:** [Database providers](https://github.com/miekhra/OpenHoldemEngine/blob/main/docs/providers.md) · [Repository](https://github.com/miekhra/OpenHoldemEngine)
