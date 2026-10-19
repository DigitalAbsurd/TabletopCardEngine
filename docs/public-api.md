# API compatibility

This page is the compatibility contract for all seven packages (`TabletopCardEngine`, `TabletopCardEngine.Sqlite`,
`TabletopCardEngine.PostgreSql`, `TabletopCardEngine.SqlServer`, `TabletopCardEngine.InMemory`,
`TabletopCardEngine.AspNetCore` and `TabletopCardEngine.OpenTelemetry`): what may change in a minor release
and how `dotnet pack` enforces it. Everything else in every assembly (`Holdem/Data/`, `Holdem/Engine/`,
`Holdem/Mappers/`, `Holdem/ActionHandlers/`) is `internal`; the tests and the demo reach it through
`InternalsVisibleTo`, which package validation ignores.

The member-by-member reference of all seven packages is the [API reference](api/index.md).

## How the baseline is enforced

The library project sets `EnablePackageValidation` and points `PackageValidationBaselinePath` at
[`eng/api-baseline/TabletopCardEngine.1.0.0-baseline.nupkg`](https://github.com/DigitalAbsurd/TabletopCardEngine/tree/main/eng/api-baseline), a package built
from the tree and committed. The gate is the plain command, with no extra properties:

```
dotnet pack src/TabletopCardEngine -c Release
```

It compares the freshly built assembly with the one inside the baseline package and fails with an
ApiCompat diagnostic on a break: `CP0001` for a removed type, `CP0002` for a removed member, and the
related `CP` rules for changed signatures, visibility or base types. `dotnet build` does not run
package validation; only `pack` does, which is why it is one of the gates.

What it does not catch: ApiCompat compares the binary surface, so a change that only breaks source
compatibility, such as removing a parameter's default value, passes. Review that by hand, in the diff
of `docs/api/` that the change produces: the generated definitions show default values and nullability.
A break that the gate reports, or a source break it misses, is a major version under
[versioning.md](versioning.md).

The baseline is a committed file rather than a published version because there is no published
version yet. Once 1.0.0 is on nuget.org, see [After 1.0.0 is on nuget.org](#after-100-is-on-nugetorg).

## The baseline is frozen

The rule for 1.x: **no breaking change; additions are allowed.** Package validation runs ApiCompat in
its default, non-strict mode, which reports removed and changed members (`CP0001`, `CP0002`, ...) and
not additions, so:

- An additive change needs **no** re-pack of any baseline. Regenerate the API reference and nothing else:

  ```
  pwsh ./scripts/generate-api-reference.ps1 -Update
  ```

- A `CP` diagnostic means the change is a break. It is not done in 1.x, and it is never "fixed" by
  re-packing the baseline. Do not set `EnableStrictModeForBaselineValidation` (it would report
  additions too), and never add a `CompatibilitySuppressions.xml` or `ApiCompatGenerateSuppressionFile`:
  a suppressed break is a baseline that no longer means anything.
- `ApiBaselineFreezeTests` pins the SHA-256 of every file in `eng/api-baseline/`, so a break hidden by
  a re-pack is a red test. Changing a pinned hash is a maintainer decision recorded in the CHANGELOG,
  never something an agent does.
- Members added after the freeze are not in the baselines, so they are not guarded until a release
  that contains them becomes the baseline. Once 1.0.0 is on nuget.org, the project switches to
  `PackageValidationBaselineVersion` (#313), which retires `ApiBaselineFreezeTests` together with
  `eng/api-baseline/`.

Every baseline in [`eng/api-baseline/`](https://github.com/DigitalAbsurd/TabletopCardEngine/tree/main/eng/api-baseline) holds one `lib/net10.0` asset, because the packages target
`net10.0` only. Package validation compares assets per target framework, so moving the packages to
another framework makes `dotnet pack` fail with `PKV006` ("Target framework net10.0 is no longer
supported in the latest version") before it compares a single member. That is the one case where
every baseline would be re-packed without a surface change; it, too, changes the pinned hashes and is
a maintainer decision.

## After 1.0.0 is on nuget.org

For the first release after the stable 1.0.0 is published on nuget.org, follow these steps:

1. In each of the seven projects of `TabletopCardEngine.Packages.slnf`, replace `PackageValidationBaselinePath` with
   `<PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion>`. From then on the
   baseline is the last stable version on nuget.org, raised after each stable minor.

2. Delete `eng/api-baseline/`, `ApiBaselineFreezeTests`, and the re-pack instructions that point at them: this section and fact 25 in
   [`task-run.md`](https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/task-run.md#engine-facts).

3. A surface change then needs no re-pack. A break against the published version fails `dotnet pack`
   with the same `CP` diagnostics, and it is a major version (see `versioning.md`).

This transition happens in the `v1.0 · NuGet packages & public launch` milestone.

### Enums are open

New members may be added to the public enums (`GameType`, `BettingStructure`, `HandActionType`,
`PlayerActionType`, `HoldemErrorCode`, `HandRankType`, `Street`, `HoldemSchemaState`,
`HoldemSchemaFailure`) in a minor release. Existing members never change value or meaning. A
consumer's `switch` over them needs a default arm.

## The provider packages

`TabletopCardEngine.Sqlite`, `TabletopCardEngine.PostgreSql`, `TabletopCardEngine.SqlServer` and
`TabletopCardEngine.InMemory` each have their own baseline,
`eng/api-baseline/TabletopCardEngine.<Provider>.1.0.0-baseline.nupkg` in
[`eng/api-baseline/`](https://github.com/DigitalAbsurd/TabletopCardEngine/tree/main/eng/api-baseline), enforced the same way by
`dotnet pack src/TabletopCardEngine.<Provider> -c Release`. Each provider's public surface is one
class of `UseHoldem*` overloads (`HoldemSqliteDbContextOptionsBuilderExtensions`,
`HoldemPostgreSqlDbContextOptionsBuilderExtensions`, `HoldemSqlServerDbContextOptionsBuilderExtensions`,
`HoldemInMemoryDbContextOptionsBuilderExtensions`); migrations and design-time factories are
`internal`. The baselines are packed without validation, because there is nothing to compare the
first one with (history, not to be run again; see [The baseline is frozen](#the-baseline-is-frozen)):

```
dotnet pack src/TabletopCardEngine.Sqlite -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
dotnet pack src/TabletopCardEngine.PostgreSql -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
dotnet pack src/TabletopCardEngine.SqlServer -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
dotnet pack src/TabletopCardEngine.InMemory -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
```

## The ASP.NET Core package

`TabletopCardEngine.AspNetCore` has its own baseline,
[`eng/api-baseline/TabletopCardEngine.AspNetCore.1.0.0-baseline.nupkg`](https://github.com/DigitalAbsurd/TabletopCardEngine/tree/main/eng/api-baseline),
enforced the same way by `dotnet pack src/TabletopCardEngine.AspNetCore -c Release`. Its public surface so far is
`HoldemHostingBuilderExtensions`, `HoldemHealthChecksBuilderExtensions`,
`HoldemHostOptions` and `HoldemEndpointRouteBuilderExtensions` (later additions do not re-pack the baseline). The baseline is packed without validation, because there is nothing to compare
the first one with (history, not to be run again; see [The baseline is frozen](#the-baseline-is-frozen)):

```
dotnet pack src/TabletopCardEngine.AspNetCore -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
```

## The OpenTelemetry package

`TabletopCardEngine.OpenTelemetry` has its own baseline,
[`eng/api-baseline/TabletopCardEngine.OpenTelemetry.1.0.0-baseline.nupkg`](https://github.com/DigitalAbsurd/TabletopCardEngine/tree/main/eng/api-baseline),
enforced the same way by `dotnet pack src/TabletopCardEngine.OpenTelemetry -c Release`. Its public surface is
`TabletopCardEngineInstrumentationExtensions` with the `AddTabletopCardEngineInstrumentation()` overloads for
`TracerProviderBuilder`, `MeterProviderBuilder` and `IOpenTelemetryBuilder`; see ADR-0002 for the architecture.
The baseline is packed without validation, because there is nothing to compare
the first one with (history, not to be run again; see [The baseline is frozen](#the-baseline-is-frozen)):

```
dotnet pack src/TabletopCardEngine.OpenTelemetry -c Release -p:MinVerVersionOverride=1.0.0-baseline -p:EnablePackageValidation=false -o eng/api-baseline
```

