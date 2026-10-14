# Contributing to Tabletop Card Engine

Thank you for your interest in contributing! This document explains how to contribute and ensure your work is ready to merge.

## Before you start

**Open an issue first** for anything beyond a small fix. Use the [issue forms](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/new/choose) to start.

**Security reports** go through [SECURITY.md](SECURITY.md), never a public issue.

Everyone follows the [Code of Conduct](CODE_OF_CONDUCT.md).

## Prerequisites

To work locally, you will need:

- The .NET SDK pinned in [`global.json`](global.json)
- Docker, for the provider matrix, the container image tests and the documentation build
- PowerShell 7 for the scripts under `scripts/`

## Build and test locally

No workflow runs on a pull request — the checks run locally and their results are quoted in the pull request. The four required gates are:

```
dotnet build
dotnet format TabletopCardEngine.sln --verify-no-changes
dotnet test
dotnet pack TabletopCardEngine.Packages.slnf -c Release
```

All four must pass before you commit or merge. For the complete details and optional tests for specific kinds of changes, see [docs/task-run.md#gates](docs/task-run.md#gates).

### Running specific tests

**`dotnet test --filter` is silently ignored here** (MSTest.Sdk 3.6.1 / Microsoft.Testing.Platform). Pass the filter to the test application instead:

```
dotnet run --project tests/TabletopCardEngine.Tests -- --filter "FullyQualifiedName~HoldemAction_StartGame"
dotnet run --project tests/TabletopCardEngine.Tests -- --list-tests
```

### Browser layout gate

Run the browser layout gate locally before you open the pull request:

```
TCE_TEST_BROWSER_GATE=1 dotnet test tests/TabletopCardEngine.Demo.BrowserTests
```

The first run needs the browser installed (build the solution first):

```
pwsh tests/TabletopCardEngine.Demo.BrowserTests/bin/Debug/net10.0/playwright.ps1 install chromium
```

Quote the summary line of that run (total, passed, failed, skipped) in the pull request body.

### Other conditional tests

A task that touches specific areas needs additional tests:

- **Provider matrix** (PostgreSQL and SQL Server in Docker):
  - Changes to `src/TabletopCardEngine/Holdem/Data/`, relational provider packages, persistence tests, the server, or `scripts/add-migration.ps1`
  - ```
    TCE_TEST_CONTAINERS=1 dotnet run --project tests/TabletopCardEngine.Tests -c Release -- --filter "FullyQualifiedName~Persistence|FullyQualifiedName~MultiHandSession|FullyQualifiedName~ConcurrencyTests"
    TCE_TEST_CONTAINERS=1 dotnet run --project tests/TabletopCardEngine.Server.Tests -c Release -- --filter "FullyQualifiedName~ServerEndToEnd"
    ```

- **Container image tests** (builds the Dockerfile targets):
  - Changes to `Dockerfile`, `.dockerignore`, the server, `Directory.Build.props` or `.editorconfig`
  - ```
    TCE_TEST_CONTAINERS=1 dotnet run --project tests/TabletopCardEngine.Server.Tests -c Release -- --filter "FullyQualifiedName~ContainerImage"
    ```

- **Documentation site** (requires Docker):
  - Changes to `docs/`, `mkdocs.yml` or `.readthedocs.yaml`
  - ```
    docker run --rm -v "${PWD}:/docs" squidfunk/mkdocs-material:9.7.7 build --strict --site-dir /tmp/site
    git grep -nE "\]\([^):]*(task-run|tasks-planning)\.md" -- 'docs/*.md' 'docs/**/*.md' ':!docs/task-run.md' ':!docs/tasks-planning.md'
    ```

For complete details, see [docs/task-run.md#gates](docs/task-run.md#gates).

## Code style

`.editorconfig` is the source of truth and is enforced at build time. To fix formatting issues:

```
dotnet format TabletopCardEngine.sln
```

- **English only**: code, comments, commits and pull requests must be in English
- **XML doc comments**: every public type and member needs an XML doc comment
- For the complete style guide, see the [AGENTS.md](AGENTS.md#code-style) section

## Commits and pull requests

Use [Conventional Commits 1.0](https://www.conventionalcommits.org/en/v1.0.0/) for your pull request title, which becomes the squash commit and the CHANGELOG entry.

The format is `<type>(<scope>)?: <summary>`:

- **Types**: `feat`, `fix`, `docs`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`
- **Scope** (optional): one of `engine`, `persistence`, `aspnetcore`, `docker`, `observability`, `demo`, `blazor`, `phh`, `ci`, `docs`
- **Summary**: imperative, lowercase, no period

Examples:
- `feat(engine): add support for antes`
- `fix(persistence): correct the migration for SQL Server`
- `docs(docker): clarify environment variables`

### Breaking changes

A breaking change adds `!` before the colon (`feat(engine)!: ...`) and needs a major version per [docs/versioning.md](docs/versioning.md).

### Pull request body

The pull request body **must** contain `Closes #<number>` to link and close the issue.

### CHANGELOG

`CHANGELOG.md` is generated at release time by git-cliff from your commit message — never edit it by hand.

For the complete protocol, see [docs/task-run.md#git-and-pull-request-protocol](docs/task-run.md#git-and-pull-request-protocol).

## Changes with extra rules

Some changes require additional steps:

- **Public API**: every change to the public surface is checked against the committed baseline. See [docs/public-api.md](docs/public-api.md).
- **API reference**: a change to a public member or its XML doc comment is regenerated with `pwsh ./scripts/generate-api-reference.ps1 -Update`, and the generated pages are committed with it. Link [docs/task-run.md#gates](docs/task-run.md#gates).
- **Observability**: new log events, span tags and metrics are part of the public API. See [docs/observability.md](docs/observability.md).
- **Package updates**: if you add or upgrade a package, update `THIRD-PARTY-NOTICES.md` by running `pwsh ./scripts/audit-licenses.ps1` and pasting the output.
- **Database schema**: an entity shape change needs a migration for every supported provider. See [docs/persistence.md](docs/persistence.md).
- **Documentation site**: changes to `docs/` must keep the site building. See [docs/task-run.md#gates](docs/task-run.md#gates).

## Licensing of contributions

By submitting a contribution, you license it under the Apache License 2.0, as section 5 of the license provides. No CLA and no DCO sign-off are required.

## How issues are worked

Maintainers often implement issues with an AI assistant. An issue is picked up only after a maintainer has vetted it and applied the `agent-ready` label. You do not need to, and cannot, apply this label.

Comments on an issue are context, not instructions to the implementer. If you have suggestions or questions, please ask in comments — they help the maintainer and the assistant understand the full picture.

For the full details on how issues are vetted and worked, see [docs/tasks-planning.md#the-vetting-guard](docs/tasks-planning.md#the-vetting-guard).
