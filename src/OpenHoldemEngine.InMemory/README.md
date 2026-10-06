# OpenHoldemEngine.InMemory

In-memory provider for `OpenHoldemEngine`. Use this for tests, prototypes, and isolated unit testing—not for production.

```bash
dotnet add package OpenHoldemEngine.InMemory --prerelease
```

Register the engine with an in-memory store. The string is a unique name for the store:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine;
using OpenHoldemEngine.InMemory;

var services = new ServiceCollection();
services.AddHoldemEngine(db => db.UseHoldemInMemory("tests"));
using var provider = services.BuildServiceProvider();

// Create the schema (stored only in memory)
await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
var engine = provider.GetRequiredService<HoldemEngine>();
```

Two engines given the same store name will share the same data. Use a unique name for each isolated engine—one per test, one per tenant, etc. The in-memory store does not persist across process restarts.

**Documentation:** [Database providers](https://github.com/miekhra/OpenHoldemEngine/blob/main/docs/providers.md) · [Repository](https://github.com/miekhra/OpenHoldemEngine)
