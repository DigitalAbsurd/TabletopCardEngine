# OpenHoldemEngine

A .NET 8 class library modelling Texas Hold'em cash games. This repository ships the engine as a library — there is no executable. Consumers register it via `services.AddHoldemEngine()` and drive everything through one method: `ApplyHoldemActionAsync(action, cancellationToken)`.

## Try the interactive demo

[**OpenHoldemEngine Blazor WebAssembly Demo**](https://miekhra.github.io/OpenHoldemEngine/)

An interactive walkthrough that shows how the engine works:

- Play hands or replay imported hand histories
- Watch the database inspector reveal the exact tables and rows the engine writes
- See the engine call log showing the exact C# code that created each state change

This teaches the core concept: **there is no in-memory game object — the database is the state**.

![OpenHoldemEngine Demo Layout](src/OpenHoldemEngine.Demo/wwwroot/img/demo-layout.svg)

## License

OpenHoldemEngine is released under the [MIT License](LICENSE). The licenses and notices of the
third-party packages it depends on, of the assets the demo redistributes, and of the vendored PHH
hand-history corpus are in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).