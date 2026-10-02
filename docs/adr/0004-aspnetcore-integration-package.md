# ADR-0004: Ship the ASP.NET Core integration as one package, OpenHoldemEngine.AspNetCore

- **Status:** Accepted
- **Date:** 2026-10-02
- **Issue:** [#262](https://github.com/miekhra/OpenHoldemEngine/issues/262), milestone
  `v1.0 · ASP.NET Core integration package`

## Context

A host that wants the engine behind HTTP, or just wired into its Generic Host, writes the same code
each time: register the engine from configuration, add a health check, migrate the schema on
start-up, turn the telemetry on, and expose `ApplyHoldemActionAsync` as a REST API. The milestone
ships that code as a package. This record fixes the decisions the milestone's later issues build on,
so that each of them does not reopen them.

## Decision

### 1. Name and shape

Three options were on the table:

- **`OpenHoldemEngine.AspNetCore`, one package — chosen.**
- `OpenHoldemEngine.Extensions.Hosting`, one package.
- Two packages: a hosting package for the configuration, health check and migration, and a web
  package for the REST part.

The reasons for the first:

- The REST part (`IEndpointRouteBuilder`, `RouteGroupBuilder`) needs `Microsoft.AspNetCore.App`
  anyway. A name ending in `Extensions.Hosting` would promise a non-web footprint the package cannot
  keep.
- Splitting it in two would put the health check into a non-web package. There
  `Microsoft.Extensions.Diagnostics.HealthChecks` is a NuGet package, not part of a shared
  framework, which breaks the milestone's rule: no NuGet dependency beyond the engine and the shared
  framework.
- The NuGet milestone already names `OpenHoldemEngine.AspNetCore`.

The cost: one more package to version, baseline and list in the notices, and a worker host that
wants only the configuration gets all of ASP.NET Core.

### 2. Dependency rule

The package targets `net10.0`, has a `FrameworkReference` to `Microsoft.AspNetCore.App`, a
`ProjectReference` to the core, and **no `PackageReference` at all**. The shared framework already
carries the `Microsoft.Extensions.*` abstractions it needs. `AspNetCorePackageTests` reads the
restored `project.assets.json` and fails on a direct package, on a framework reference other than
`Microsoft.AspNetCore.App`, and on any `OpenTelemetry*` library in the graph.

### 3. Observability

The one-line OpenTelemetry wiring lives in `OpenHoldemEngine.OpenTelemetry`, as
`IOpenTelemetryBuilder.AddOpenHoldemEngineInstrumentation()`, through
`OpenTelemetry.Api.ProviderBuilderExtensions` (Apache-2.0). It does not live in this package, so
this package never references OpenTelemetry. That gives the OpenTelemetry package a second package
reference, which amends [ADR-0002](0002-opentelemetry-instrumentation-package.md)'s
"one package reference". ADR-0002 itself is not rewritten.

### 4. The HTTP contract

- **JSON.** The engine's enums are written as their names, and `Card` as `"ah"`. Type-specific
  converters are registered into the host's `Microsoft.AspNetCore.Http.Json.JsonOptions`; no other
  host type is touched.
- **Failures** are `ProblemDetails`, with `code` holding the `HoldemErrorCode` name.
- **Statuses.**
  - `404` for `GameNotFound`.
  - `409` for the state codes: `GameAlreadyExists`, `CurrentHandEnded`, `PreviousHandNotEnded`,
    `GameAlreadyStarted`, `GameNotStarted`, `GameFinished`.
  - `412` for `Conflict` when the request carried `If-Match`, `409` without.
  - `422` for the request codes: `InvalidAction`, `UnsupportedGameOptions`, `InvalidGameOptions`,
    `InvalidSeat`, `IllegalAction`.
  - `400` is left to the framework: malformed JSON, binding failures, a malformed `If-Match`.
- **Concurrency.** `ETag: "<Version>"`.

### 5. Security stance

The API returns `GameData` exactly as the engine does, every seat's hole cards included. It is a
trusted, game-master API, and the host protects it, for example with `RequireAuthorization()` on the
returned group. Hiding cards per viewer is not in v1.

## Consequences

- **A fourth shipped package.** It needs the same package metadata, README and license as the
  others, its own API baseline in `eng/api-baseline/`, and a section in `THIRD-PARTY-NOTICES.md`
  (which lists no package for it).
- **All of ASP.NET Core for a worker host** that only wants the configuration overload.
- **The core grants it `InternalsVisibleTo`**, as it does for the provider packages, so the
  configuration overload can reach the internal `HoldemEngineOptions`.
- **Any first-party package a later issue wants is a decision of its own.** The rule is the dependency
  guard above, not a convention.
