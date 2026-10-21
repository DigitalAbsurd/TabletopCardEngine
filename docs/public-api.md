# API compatibility

This page is the compatibility contract for all seven packages (`TabletopCardEngine`, `TabletopCardEngine.Sqlite`,
`TabletopCardEngine.PostgreSql`, `TabletopCardEngine.SqlServer`, `TabletopCardEngine.InMemory`,
`TabletopCardEngine.AspNetCore` and `TabletopCardEngine.OpenTelemetry`): what may change in a minor release
and how `dotnet pack` enforces it. Everything else in every assembly (`Holdem/Data/`, `Holdem/Engine/`,
`Holdem/Mappers/`, `Holdem/ActionHandlers/`) is `internal`; the tests and the demo reach it through
`InternalsVisibleTo`, which package validation ignores.

The member-by-member reference of all seven packages is the [API reference](api/index.md).

## How the baseline is enforced

Each of the seven packable projects sets `EnablePackageValidation` and points `PackageValidationBaselineVersion`
at `1.0.0`, the last stable version on nuget.org. The gate is the plain command, with no extra properties:

```
dotnet pack src/TabletopCardEngine -c Release
```

It compares the freshly built assembly against the baseline package downloaded from nuget.org and fails with an
ApiCompat diagnostic on a break: `CP0001` for a removed type, `CP0002` for a removed member, and the
related `CP` rules for changed signatures, visibility or base types. `dotnet build` does not run
package validation; only `pack` does, which is why it is one of the gates.

What it does not catch: ApiCompat compares the binary surface, so a change that only breaks source
compatibility, such as removing a parameter's default value, passes. Review that by hand, in the diff
of `docs/api/` that the change produces: the generated definitions show default values and nullability.
A break that the gate reports, or a source break it misses, is a major version under
[versioning.md](versioning.md).

## The baseline is enforced after each release

The rule for 1.x and beyond: **no breaking change; additions are allowed.** Package validation runs ApiCompat in
its default, non-strict mode, which reports removed and changed members (`CP0001`, `CP0002`, ...) and
not additions, so:

- An additive change needs **no** re-pack of any baseline. Regenerate the API reference and nothing else:

  ```
  pwsh ./scripts/generate-api-reference.ps1 -Update
  ```

- A `CP` diagnostic means the change is a break. In 1.x and beyond, a break fails the pack and must be
  reverted or released in a new major version. Do not set `EnableStrictModeForBaselineValidation` (it would report
  additions too), and never add a `CompatibilitySuppressions.xml` or `ApiCompatGenerateSuppressionFile`:
  a suppressed break is a baseline that no longer means anything.
- After each stable release (e.g., `1.1.0`), update `PackageValidationBaselineVersion` to that version
  in all seven packable projects. A break against the newly published baseline is then a major version under
  [versioning.md](versioning.md).

### Enums are open

New members may be added to the public enums (`GameType`, `BettingStructure`, `HandActionType`,
`PlayerActionType`, `HoldemErrorCode`, `HandRankType`, `Street`, `HoldemSchemaState`,
`HoldemSchemaFailure`) in a minor release. Existing members never change value or meaning. A
consumer's `switch` over them needs a default arm.

## The provider packages

`TabletopCardEngine.Sqlite`, `TabletopCardEngine.PostgreSql`, `TabletopCardEngine.SqlServer` and
`TabletopCardEngine.InMemory` each have their own baseline enforced the same way by
`dotnet pack src/TabletopCardEngine.<Provider> -c Release`. Each provider's public surface is one
class of `UseHoldem*` overloads (`HoldemSqliteDbContextOptionsBuilderExtensions`,
`HoldemPostgreSqlDbContextOptionsBuilderExtensions`, `HoldemSqlServerDbContextOptionsBuilderExtensions`,
`HoldemInMemoryDbContextOptionsBuilderExtensions`); migrations and design-time factories are
`internal`. The baseline is the last stable version on nuget.org, updated after each release (see [The baseline is enforced after each release](#the-baseline-is-enforced-after-each-release)).

## The ASP.NET Core package

`TabletopCardEngine.AspNetCore` has its own baseline enforced the same way by
`dotnet pack src/TabletopCardEngine.AspNetCore -c Release`. Its public surface so far is
`HoldemHostingBuilderExtensions`, `HoldemHealthChecksBuilderExtensions`,
`HoldemHostOptions` and `HoldemEndpointRouteBuilderExtensions`. The baseline is the last stable version on nuget.org,
updated after each release (see [The baseline is enforced after each release](#the-baseline-is-enforced-after-each-release)).

## The OpenTelemetry package

`TabletopCardEngine.OpenTelemetry` has its own baseline enforced the same way by
`dotnet pack src/TabletopCardEngine.OpenTelemetry -c Release`. Its public surface is
`TabletopCardEngineInstrumentationExtensions` with the `AddTabletopCardEngineInstrumentation()` overloads for
`TracerProviderBuilder`, `MeterProviderBuilder` and `IOpenTelemetryBuilder`; see ADR-0002 for the architecture.
The baseline is the last stable version on nuget.org, updated after each release (see [The baseline is enforced after each release](#the-baseline-is-enforced-after-each-release)).

