# OpenHoldemEngine

A .NET 10 class library modelling Texas Hold'em cash games. This repository ships the engine as a library — there is no executable. Consumers register it via `services.AddHoldemEngine()` and drive everything through one method: `ApplyHoldemActionAsync(action, cancellationToken)`.

## Supported platform

The libraries target .NET 10 (LTS), supported through November 14, 2028.

## Try the interactive demo

[**OpenHoldemEngine Blazor WebAssembly Demo**](https://miekhra.github.io/OpenHoldemEngine/)

An interactive walkthrough that shows how the engine works:

- Play hands or replay imported hand histories
- Watch the database inspector reveal the exact tables and rows the engine writes
- See the engine call log showing the exact C# code that created each state change

This teaches the core concept: **there is no in-memory game object — the database is the state**.

![OpenHoldemEngine Demo Layout](src/OpenHoldemEngine.Demo/wwwroot/img/demo-layout.svg)

## Docker

The OpenHoldemEngine server is available as a Docker image for SQLite, PostgreSQL, and SQL Server.
Build the image locally from the `Dockerfile` at the repository root.
There is no API authentication in the container, so run the image on a trusted network or behind an authenticating reverse proxy.
See [docs/docker.md](docs/docker.md) for the full guide.

## Roadmap

The v1.0 API is no-limit Texas Hold'em cash games at 2 to 6 seats. What comes next and in which order is in [docs/roadmap.md](docs/roadmap.md).

## License

OpenHoldemEngine is released under the [Apache License, Version 2.0](LICENSE). Commercial use
is allowed. Anyone who redistributes the engine or a work derived from it must keep the attribution
in [NOTICE](NOTICE), as section 4(d) of the license requires.
[ADR-0001](docs/adr/0001-project-license.md) explains why this license was chosen.

The licenses and notices of the third-party packages it depends on, of the assets the demo
redistributes, and of the vendored PHH hand-history corpus are in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).