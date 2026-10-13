# Welcome to TabletopCardEngine

TabletopCardEngine is a .NET 10 class library modelling Texas Hold'em cash games. There is no executable — consumers register it via `services.AddHoldemEngine()` and drive everything through one method: `IHoldemTable.ApplyAsync(action, cancellationToken)`. The core concept is simple: **there is no in-memory game object — the database is the state**.

## What's included

**v1.0 scope**: No-Limit Texas Hold'em cash games, 2 to 6 seated players, no antes, no rake.

The [roadmap](roadmap.md) covers other structures, more seats, antes and tournaments in later releases.

## Try the interactive demo

[**TabletopCardEngine Blazor WebAssembly Demo**](https://digitalabsurd.github.io/TabletopCardEngine/)

An interactive walkthrough that shows how the engine works:

- Play hands or replay imported hand histories
- Watch the database inspector reveal the exact tables and rows the engine writes
- See the engine call log showing the exact C# code that created each state change

## Where to go next

- **[Getting started](getting-started.md)** — prerequisites and your first hand
- **[Key concepts](concepts/index.md)** — the core ideas: one method, the database is the state, deterministic dealing, and optimistic concurrency
- **[Database providers](providers.md)** — choosing and configuring a database
- **[Migrations](migrations.md)** — creating and upgrading the database schema
- **[Guides](aspnetcore.md)** — usage patterns, container images, and observability
- **[Reference](public-api.md)** — the public API and supported databases
- **[Project](versioning.md)** — versioning, support, roadmap, and architecture decisions
