# Semantic Versioning and breaking changes

## What this document is

This document states the versioning policy for the Holdem engine and its packages: which changes are breaking, and how the database schema version relates to the package version. It is for maintainers, release engineers, and consumers who need to know whether an upgrade is safe. See also [docs/release-process.md](release-process.md) for how a release is cut and tagged, and [docs/support-policy.md](support-policy.md) for how long versions are supported.

## Semantic Versioning

The packages follow [Semantic Versioning 2.0.0](https://semver.org/). **One version for everything (lockstep):** the seven packages in `TabletopCardEngine.Packages.slnf` (`TabletopCardEngine`, `.OpenTelemetry`, `.AspNetCore`, `.Sqlite`, `.PostgreSql`, `.SqlServer`, `.InMemory`) and the Docker images all get the same version from one git tag `vX.Y.Z` or `vX.Y.Z-preview.N` / `vX.Y.Z-rc.N`. A package with no change since the last release still gets the new version.

The pre-release ladder for 1.0 is `1.0.0-preview.1`, then `1.0.0-preview.2`, ..., then `1.0.0-rc.1`, `1.0.0-rc.2`, ..., then `1.0.0`. The public API of 1.0 is frozen from `1.0.0-preview.1` on. When a pre-release breaks a prior pre-release, the maintainers record the reason in the CHANGELOG.

## What is a breaking change (major)

A major version increment (e.g., `1.0.0` → `2.0.0`) is required for any breaking change. A breaking change is any of:

- **The public .NET surface** — whatever `dotnet pack TabletopCardEngine.Packages.slnf -c Release` reports with a `CP` diagnostic (`CP0001`, `CP0002`, ...) per [docs/public-api.md](public-api.md). Also source-only breaks that package validation cannot detect, such as removing a parameter's default value or changing a member's optional parameter.

- **The numeric value or meaning of a `HoldemErrorCode`** — the stable machine-readable codes in the `HoldemErrorCode` enum (`GameAlreadyExists`, `CurrentHandEnded`, etc.). Changing a code's value or its semantic meaning is a break.

- **An event ID, a span tag's name or meaning, or an instrument's name or unit** — the logs, traces, and metrics defined in `Holdem/Diagnostics/` and documented in [docs/observability.md](observability.md), with event IDs defined in `HoldemLog.cs`, span tags in `HoldemActivity.cs`, and instrument names and units in `HoldemMetrics.cs`.

- **Rule behaviour a caller can observe** — a change such that the same sequence of `HoldemAction`s on the same deck produces different `GameData` (legal actions, positions, pot split, rounding, card order). A fix of a rule that was plainly wrong is a patch, but the CHANGELOG entry must clearly say what changed and why the prior behaviour was wrong.

- **A schema change that `IHoldemSchemaManager.MigrateAsync` cannot apply by itself, or one that loses data** — see [The database schema and the package version](#the-database-schema-and-the-package-version) below.

## Minor and patch

A minor version increment (e.g., `1.0.0` → `1.1.0`) is used for:

- New public members in a type (methods, properties, records)
- New `HoldemErrorCode` values, appended at the end (so existing `switch` statements remain correct)
- New log events, span tags, or metric instruments
- New options with defaults that keep today's behaviour
- New betting structures, game types, or seat counts that plug into `GameOptionData` (once those are supported)

A patch version increment (e.g., `1.0.0` → `1.0.1`) is used for:

- Bug fixes with no observable contract change — internal fixes, performance improvements, and defect repairs that do not alter what the public API returns for the same inputs.

New features never require a major version: "v2" means "breaking", not "bigger".

## The database schema and the package version

`HoldemSchema.Version` is an integer of its own, bumped by one in the same commit as every migration. It is not derived from the package version. `HoldemSchema.EngineVersion` is the release (without a pre-release suffix) that introduces that schema version: `"1.0.0"` for both `1.0.0-preview.1` and the final `1.0.0`, and it is included in the database row `__TceSchema` so older versions can be identified.

**A migration requires at least a minor release; it is never a patch.** There is no downgrade: once a database is migrated, an older engine refuses it with `HoldemSchemaException(DatabaseIsNewer)`. A patch release could not run on a database migrated by a later patch of the same minor, and that would leave a user stuck. A migration that `MigrateAsync` applies on its own and that does not lose any data is a minor release. Anything else — a migration that loses data, one that needs manual steps, or one that changes observable rule behaviour — is major (see [What is a breaking change (major)](#what-is-a-breaking-change-major) above).

For the database rule and its schema versioning, see [docs/persistence.md](persistence.md#schema-versioning).
