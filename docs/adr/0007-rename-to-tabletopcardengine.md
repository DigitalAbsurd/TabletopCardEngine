# ADR-0007: Rename to TabletopCardEngine

- **Status:** Accepted; §3 and the "no other" clause of §4 superseded by [ADR-0008](0008-harmonize-the-public-api.md)
- **Date:** 2026-10-06
- **Issue:** [#389](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/389), milestone
  `v1.0 · Rename to TabletopCardEngine`

## Context

The project name collides with **OpenHoldem** (https://github.com/OpenHoldem, SourceForge `openholdem`),
a long-established GPLv3 C++ framework for online-poker bots that describes itself as a "programmable
logic engine". A web search for `"OpenHoldemEngine"` returns only OpenHoldem pages. The collision risks
confusion ("the OpenHoldem engine"), association with ToS-violating bots, and a suspicion of GPL
derivation, which this repository deliberately avoids.

The maintainer has chosen a new name, `TabletopCardEngine`, and measured availability:

- **NuGet.org:** `TabletopCardEngine` is free
  (`https://api.nuget.org/v3-flatcontainer/tabletopcardengine/index.json` → 404);
  `openholdemengine` is not listed (404).
- **GitHub:** `DigitalAbsurd/TabletopCardEngine` is the new repository location
  (redirected from `miekhra/OpenHoldemEngine` on 2026-10-07).
- **Docker Hub:** `digitalabsurd/tabletopcardengine` is free; `miekhra/openholdemengine` is not listed.
- **Release history:** only the private pre-release `v1.0.0-preview.1` exists; the repository was
  private throughout.

The rename needs two one-off exceptions to rules this repository otherwise holds absolute:
[docs/public-api.md](../public-api.md#the-baseline-is-frozen) requires that changing a pinned
baseline hash be a maintainer decision recorded in the CHANGELOG, never something an agent does;
and applied migrations are never edited. Both exceptions are authorized and recorded here.

## Decision

### 1. The new name is `TabletopCardEngine`

This resolves the collision with OpenHoldem, frees the NuGet ID, and reflects that the engine models
a card game, not online poker specifically.

### 2. Name mappings

These mappings are applied throughout the codebase:

| Old | New | Where |
|---|---|---|
| `https://github.com/miekhra/OpenHoldemEngine`, `miekhra/OpenHoldemEngine` | `https://github.com/DigitalAbsurd/TabletopCardEngine`, `DigitalAbsurd/TabletopCardEngine` | Repository URLs, `mkdocs.yml` `repo_name`, the runner's `$Repo` default |
| `https://miekhra.github.io/OpenHoldemEngine/` | `https://digitalabsurd.github.io/TabletopCardEngine/` | Demo GitHub Pages URL |
| `miekhra/openholdemengine` | `digitalabsurd/tabletopcardengine` | Docker Hub repository |
| `OpenHoldemEngine` | `TabletopCardEngine` | Namespaces, assemblies, package IDs, folders, solution, telemetry names, prose, URLs |
| `openholdemengine` | `tabletopcardengine` | Docker image names, health check default name, lowercase URLs |
| `OpenHoldem Engine` | `Tabletop Card Engine` | Prose title (`CONTRIBUTING.md`) |
| `OHE_` (env vars), `TCE0001` (diagnostic IDs) | `TCE_`, `TCE0001` | Tests, scripts, docs, workflows |
| `ohe` (database schema), `tce_` (test databases), `ohe-` (storage keys, temp paths) | `tce`, `tce_`, `tce-` | Engine, migrations, tests, demo |
| `__TceSchema`, `__TceMigrationsHistory`, `TceSchemaRow` | `__TceSchema`, `__TceMigrationsHistory`, `TceSchemaRow` | Engine, migrations, tests |

The owner-qualified rows (`miekhra/...` → `DigitalAbsurd/...`) are applied first, because the plain
`OpenHoldemEngine` row would otherwise turn `miekhra/OpenHoldemEngine` into `miekhra/TabletopCardEngine`,
an owner that no longer holds the repository.

### 3. What is deliberately not renamed

The following names refer to the game, not the brand, and are kept:

- `HoldemEngine` — the public class
- `HoldemAction*` — action types and records
- `HoldemDbContext` — the Entity Framework context
- `HoldemErrorCode` — error codes
- `UseHoldem*` — configuration extension methods
- `AddHoldemEngine*` — service collection extensions
- `MapHoldemEngineApi` — API mapping
- The `holdem-data` volume — Docker volume
- The `holdem` database name

Renaming these would be a design change, not a rename. The copyright holder in `NOTICE`
(`Copyright 2025-2026 Mikhail Khrapunov`) remains unchanged: moving the repository to an
organization changes where it is hosted, not who holds the copyright. Only the project name and the
URL in `NOTICE` change.

### 3a. Amendment (2026-10-07): configuration keys

The maintainer reversed the part of §3 that kept the `HoldemEngine` configuration section and the
`ConnectionStrings__Holdem` key as game names, and decided to rename the container host's keys before
the public launch, because they are the image's user-facing surface. The `HoldemServer` key, which §3
did not mention, is renamed with them. The host has no public API, so no baseline changes.

| Old key (environment variable) | New key (environment variable) |
|---|---|
| `ConnectionStrings:Holdem` (`ConnectionStrings__Holdem`) | `ConnectionStrings:TabletopCardEngine` (`ConnectionStrings__TabletopCardEngine`) |
| `HoldemServer:Provider` (`HoldemServer__Provider`) | `TabletopCardEngineServer:Provider` (`TabletopCardEngineServer__Provider`) |
| section `HoldemEngine` (`HoldemEngine__*`), bound to `HoldemEngineHostOptions` | section `TabletopCardEngine` (`TabletopCardEngine__*`) |

### 4. Exception 1: API baselines were re-packed once

Package validation compares types by full name, so after the namespace changes every public type read
as removed (`CP0001`) against the old baselines. The seven files in `eng/api-baseline/` were replaced
by `TabletopCardEngine*.1.0.0-baseline.nupkg` packed from the renamed code, and were pinned in
`ApiBaselineFreezeTests` in that pull request. The re-pack was acceptable only because
the public inventory was unchanged except for the name (checked by `scripts/list-public-api.ps1`), and
it reached `CHANGELOG.md` the only way anything does: the rename's squash commit was a breaking
Conventional Commit (`refactor!:` with a `BREAKING CHANGE:` footer naming the re-pack), which
git-cliff filed under **Breaking changes**. Once `1.0.0` was published on nuget.org, the baselines
switched to the published version and `eng/api-baseline/` was deleted (#468). This ADR authorized that one re-pack and no other.

### 5. Exception 2: `InitialCreate` migrations are edited in place

Rather than followed by a renaming migration, `InitialCreate` is edited in place for SQLite,
PostgreSQL and SQL Server. No database outside this repository's tests has ever been migrated, so
there is nothing to upgrade, and an `InitialCreate` that creates `ohe` tables only to rename them
would be shipped forever.

### 6. What the maintainer does outside the batch

Before the rename batch runs, the maintainer:

- Renames the GitHub repository to `DigitalAbsurd/TabletopCardEngine` (completed 2026-10-07).
- Reserves the `TabletopCardEngine` ID prefix on nuget.org.
- Creates the Docker Hub repository `digitalabsurd/tabletopcardengine`.
- Creates the Read the Docs project under the new name.
- Decides the fate of the `v1.0.0-preview.1` release and tag, whose assets carry the old package IDs.

## Consequences

- **The old name stays only in git history.** Commits that carried the old name are part of the
  codebase's record and show the project's origin; they are never rewritten.
- **This ADR keeps the decision.** A later implementer looking at why the names differ will find it
  here, with the collision and the measurements on record.
- **A guard test keeps the old name out of the tree.** After the rename, a test asserts that no
  `.cs`, `.csproj`, `.sln`, `.md`, `.yml`, `.yaml`, `.ps1` file in the tree contains `OpenHoldemEngine`
  (except in comments naming history, and in `CHANGELOG.md`), so a regression in future tasks cannot
  reintroduce the old name.
