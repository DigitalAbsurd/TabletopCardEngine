# TabletopCardEngine.InMemory

In-memory provider for `TabletopCardEngine`. Use this for tests, prototypes, and isolated unit testing—not for production.

```bash
dotnet add package TabletopCardEngine.InMemory --prerelease
```

Register the engine with an in-memory store. The string is a unique name for the store:

```csharp
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.InMemory;

var services = new ServiceCollection();
services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemInMemory("tests")));
using var provider = services.BuildServiceProvider();

// Create the schema (stored only in memory)
await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
var engine = provider.GetRequiredService<IHoldemTable>();
```

Two engines given the same store name will share the same data. Use a unique name for each isolated engine—one per test, one per tenant, etc. The in-memory store does not persist across process restarts.

**Documentation:** [Database providers](https://tabletopcardengine.readthedocs.io/en/stable/providers/) · [Repository](https://github.com/DigitalAbsurd/TabletopCardEngine)
