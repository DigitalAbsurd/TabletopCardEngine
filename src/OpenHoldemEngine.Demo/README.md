# OpenHoldemEngine Blazor WebAssembly Demo

This is a walking skeleton demonstrating the `OpenHoldemEngine` library running in the browser via Blazor WebAssembly with an in-memory SQLite database.

## Building

### Prerequisites

- .NET 8.0 SDK
- The `wasm-tools-net8` workload (for native SQLite in WebAssembly)

### Install the workload

```bash
dotnet workload restore
```

This installs the Emscripten toolchain and build tools required for native library compilation in WebAssembly.

### Build in Debug configuration

```bash
dotnet build
```

### Publish in Release configuration

```bash
dotnet publish -c Release
```

The publish output goes to `bin/Release/net8.0/publish/`.

## Running the demo

### Development server

```bash
dotnet watch run
```

This launches the Blazor dev server and watches for changes.

### Accessing the demo

Once running, open the demo in your browser at `https://localhost:5001`.

Navigate to the `/diagnostics` page to verify that the engine is operational and the SQLite database is working correctly.

## Performance metrics

These measurements are from the current build configuration and serve as a baseline for detecting regressions in future changes.

### Published size (Release configuration)

- **Total: ~53.4 MB**
  - Blazor framework and runtime: ~20 MB (dotnet.js, dotnet.runtime.js, dotnet.native.wasm, ICU data)
  - .NET assemblies and dependencies: ~20 MB
  - SQLite and EF Core: ~6 MB
  - HandEvaluator library (old, static lookup tables): ~280 KB

The large published size is a known characteristic of Blazor WebAssembly applications. The Emscripten-built native SQLite binary contributes significantly to this.

### Cold-start time

Cold-start latency is dominated by:

1. **Initial load**: Network download of the .wasm file and IL→JIT compilation
2. **HandEvaluator initialization**: The library builds large static lookup tables at type initialization
3. **Database schema creation**: `HoldemDbContext.Database.EnsureCreated()` executes on app startup

First-hand creation after startup typically takes 1–2 seconds due to the HandEvaluator initialization. Subsequent operations are significantly faster.

## Architecture

### DemoEngineHost

`Engine/DemoEngineHost.cs` configures the engine for WebAssembly:

- Creates and opens a kept-alive `SqliteConnection` with `DataSource=:memory:`
- Registers it with dependency injection as both the EF Core provider and a singleton for direct access
- Calls `Database.EnsureCreated()` to initialize the schema

The kept-alive connection is critical: without it, each new `DbContext` instance would see an empty in-memory SQLite database (each connection has its own in-memory database).

### Diagnostics page

`Pages/Diagnostics.razor` verifies the engine is working:

1. Creates a game via `HoldemEngine.ApplyHoldemActionAsync(HoldemActionCreateGame, ...)`
2. Queries `sqlite_master` directly to list the engine's tables
3. Displays the nine tables the engine creates

## Known limitations and warnings

### SQLite varargs in WebAssembly

The WASM build produces warnings about SQLite varargs (`sqlite3_config`, `sqlite3_db_config`):

```
Found a native function (sqlite3_config) with varargs in e_sqlite3. Calling such functions is not supported, and will fail at runtime.
```

This is a tooling limitation (Emscripten cannot generate correct calling conventions for varargs functions in WASM). These specific functions are not called by the engine or EF Core in normal operation, so the warnings are informational. If a call to one of these functions is attempted at runtime, it will fail with a clear error.

### HandEvaluator library size

The `HandEvaluator` 1.0.1 library is old and lacks trimming annotations. It builds large static lookup tables at type initialization, contributing significantly to both published size and cold-start time. This is acceptable for a demo but should be tracked if the application grows.

## Attribution

### PHH Dataset

The sample hand histories bundled in `wwwroot/sample/` are derived from the **PHH dataset** (Poker Hand Histories).

- **Source**: https://github.com/uoftcprg/phh-dataset
- **License**: MIT
- **Copyright**: University of Toronto Computer Poker Research Group

The full license text is available in `wwwroot/sample/LICENSE-phh-dataset.txt`.

## Troubleshooting

### "wasm-tools workload not installed"

Run `dotnet workload restore` in the repository root.

### "The engine is not responding" in the diagnostics page

This usually indicates a JavaScript error or an unhandled exception in the C# code. Check the browser console (F12 Developer Tools) for details.

### Build failures with native compilation

The WASM native build can be fragile. If you encounter linker errors or unexpected warnings:

1. Clean everything: `dotnet clean && rm -r bin obj`
2. Restore workloads: `dotnet workload restore`
3. Build again: `dotnet build`

If issues persist, check the [Emscripten documentation](https://emscripten.org/docs/) and the [Blazor WebAssembly documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly).
