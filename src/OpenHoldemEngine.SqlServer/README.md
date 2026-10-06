# OpenHoldemEngine.SqlServer

SQL Server provider for `OpenHoldemEngine`. Use this for production deployments.

```bash
dotnet add package OpenHoldemEngine.SqlServer --prerelease
```

Register the engine with SQL Server. Note the `TrustServerCertificate=True` flag: most container SQL Server images use self-signed certificates, and this flag skips validation without disabling encryption.

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine;
using OpenHoldemEngine.SqlServer;

var services = new ServiceCollection();
var connectionString = "Server=.;Database=holdem;Trusted_Connection=True;TrustServerCertificate=True";
services.AddHoldemEngine(db => db.UseHoldemSqlServer(connectionString));
using var provider = services.BuildServiceProvider();

// Create or upgrade the database schema
await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
var engine = provider.GetRequiredService<HoldemEngine>();
```

All tables live in the `ohe` schema. It is recommended to dedicate a database or at least a login to the engine.

**Documentation:** [Database providers](https://github.com/miekhra/OpenHoldemEngine/blob/main/docs/providers.md) · [Repository](https://github.com/miekhra/OpenHoldemEngine)
