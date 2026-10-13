# TabletopCardEngine.SqlServer

SQL Server provider for `TabletopCardEngine`. Use this for production deployments.

```bash
dotnet add package TabletopCardEngine.SqlServer --prerelease
```

Register the engine with SQL Server. Note the `TrustServerCertificate=True` flag: most container SQL Server images use self-signed certificates, and this flag skips validation without disabling encryption.

```csharp
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.SqlServer;

var services = new ServiceCollection();
var connectionString = "Server=.;Database=holdem;Trusted_Connection=True;TrustServerCertificate=True";
services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemSqlServer(connectionString)));
using var provider = services.BuildServiceProvider();

// Create or upgrade the database schema
await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
var engine = provider.GetRequiredService<IHoldemTable>();
```

All tables live in the `tce` schema. It is recommended to dedicate a database or at least a login to the engine.

**Documentation:** [Database providers](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/providers.md) · [Repository](https://github.com/DigitalAbsurd/TabletopCardEngine)
