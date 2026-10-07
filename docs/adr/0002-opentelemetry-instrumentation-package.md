# ADR-0002: Ship the OpenTelemetry instrumentation in its own package

- **Status:** Accepted
- **Date:** 2026-09-30
- **Issue:** [#186](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/186), milestone
  `v1.0 · Logging & observability (OpenTelemetry)`

## Context

The engine publishes one `ActivitySource` and one `Meter`, both named `"TabletopCardEngine"` and
exposed as `HoldemEngineTelemetry.ActivitySourceName` and `HoldemEngineTelemetry.MeterName`. It also
logs through `ILogger`. A host that runs the OpenTelemetry SDK turns the traces and metrics on by
subscribing to those names. The idiomatic one-liner for that is an extension method,
`AddTabletopCardEngineInstrumentation()`, on `TracerProviderBuilder` and `MeterProviderBuilder`.

Those builder types live in `OpenTelemetry.Api`. The milestone keeps the core library free of any
OpenTelemetry package: it may depend only on `System.Diagnostics` and the `Microsoft.Extensions`
abstractions. So the extension cannot live in `TabletopCardEngine`, and the question is where it lives,
if anywhere.

## Options considered

### A separate package, `TabletopCardEngine.OpenTelemetry` — chosen

A small project that references the library and `OpenTelemetry.Api`, and nothing else. It holds one
public class with the two extension methods.

- Any host gets the one-liner: a Generic Host, a worker service or a console application, none of
  which has to pull ASP.NET Core in to get it.
- The ASP.NET Core package, which has a milestone of its own, can reference this one instead of
  repeating it.
- The dependency points one way, package to core, so the core stays free of OpenTelemetry. A test
  (`Core_DoesNotDependOnOpenTelemetry`) reads the core's restored assets file and fails the moment
  any `OpenTelemetry*` package appears in its graph.

### The ASP.NET Core package

Put the extension into `TabletopCardEngine.AspNetCore`. There is one package fewer, but a host that is
not a web application would have to take ASP.NET Core to get a two-line method. The extension would
also wait for that package's milestone.

### No extension, only the documented names

Ship nothing, and have hosts write `AddSource(HoldemEngineTelemetry.ActivitySourceName)` and
`AddMeter(HoldemEngineTelemetry.MeterName)` themselves. It costs nothing to maintain, and the names
are public already. But it is not how .NET instrumentation is usually offered, and each host repeats
the two calls and has to find out that both are needed.

## Decision

Ship the extension in a separate package, **`TabletopCardEngine.OpenTelemetry`**, in namespace
`TabletopCardEngine.OpenTelemetry`. It references `OpenTelemetry.Api` only, never the SDK or an
exporter: everything the extension needs is in the API, and the host brings the SDK. Logging gets no
extension, because a host already enables the `TabletopCardEngine` categories in its logging
configuration and adds the SDK's `AddOpenTelemetry()` logging provider if it wants OTLP logs.

## Consequences

- **A third package joins the shipped set.** The NuGet milestone names `TabletopCardEngine` and
  `TabletopCardEngine.AspNetCore`; this record adds `TabletopCardEngine.OpenTelemetry` to it. It needs
  the same package metadata, README and license as the others.
- **One more project to version.** Its version has to move with the library's, since it compiles
  against the library's public constants. The Versioning milestone owns how.
- **No API baseline yet.** Package validation stays off for this project until it has shipped once,
  because there is no earlier version to compare with.
- **`System.Diagnostics.DiagnosticSource` rises in hosts that take this package.** `OpenTelemetry.Api`
  1.19.1 depends on `System.Diagnostics.DiagnosticSource` 10.0.0, while the core gets a compatible version through
  EF Core 10.x. NuGet resolves the appropriate version in a host that references this package; the core's own
  graph is unchanged.
