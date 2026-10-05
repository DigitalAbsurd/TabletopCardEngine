# ADR-0005: Container host and images

- **Status:** Accepted
- **Date:** 2026-10-03
- **Issue:** [#292](https://github.com/miekhra/OpenHoldemEngine/issues/292), milestone
  `v1.0 · Docker images`

## Context

The engine ships as a library, but users who want to run it in a container need a host: a .NET
executable that registers the engine, connects to a database, and exposes it over HTTP. This
record fixes the decisions the milestone's later issues build on, so that each of them does not
reopen them.

## Decision

### 1. A separate host project, `src/OpenHoldemEngine.Server`

Three options were on the table:

- **`src/OpenHoldemEngine.Server`, a separate host project — chosen.** A .NET 10 executable
  (`Microsoft.NET.Sdk.Web`, `IsPackable=false`) with tests in `tests/OpenHoldemEngine.Server.Tests`.
- `OpenHoldemEngine.AspNetCore` itself as the host. The package is a packable library with an API
  baseline and no `PackageReference` (ADR-0004, guarded by `AspNetCorePackageTests`). An image
  needs an executable that references all three providers and an OpenAPI generator. Putting the
  executable inside the package would make it a non-packable host, which breaks the package's
  contract.
- `samples/AspNetCoreSample` as the host. The sample is the minimal example of the package. It
  has no `PackageReference` on purpose and stays as is, to keep the package's dependency rule
  clean.

### 2. One binary for all three providers

Two options were on the table:

- **One binary, with the provider picked at start-up — chosen.** The provider is read from the
  `HoldemServer__Provider` environment variable at start-up (`Sqlite`, `PostgreSql`, or
  `SqlServer`), and the host initializes its database connection accordingly. Each Docker image
  sets its own default for that variable.
- A separate build per provider, with conditional project references and `#if` directives. The
  cost is higher: three builds instead of one, a more complex project file with conditional
  references, and code paths that the tests never exercise together. The savings are about 10 MB
  per image on a 340 MB base, which is not worth the added complexity.

### 3. Environment contract

The host accepts these variables:

| Variable | Required | Values | Default | Notes |
|---|---|---|---|---|
| `HoldemServer__Provider` | Yes | `Sqlite` \| `PostgreSql` \| `SqlServer` | — | Each image sets its own |
| `ConnectionStrings__Holdem` | Yes | Connection string | — | `sqlite` image sets `Data Source=/data/holdem.db`; others have no default |
| `HoldemEngine__MigrateDatabaseOnStartup` | No | `true` \| `false` | `false` | Set to `true` in all images |
| `HoldemEngine__GamePlayersMinCount` | No | 2–6 | 2 | Existing `HoldemEngineHostOptions` setting |
| `HoldemEngine__GamePlayersMaxCount` | No | 2–6 | 6 | Existing `HoldemEngineHostOptions` setting |
| `HoldemEngine__EnableSensitiveCardLogging` | No | `true` \| `false` | `false` | Existing `HoldemEngineHostOptions` setting |
| `ASPNETCORE_HTTP_PORTS` | No | Port number | 8080 | The base image's default |
| `Logging__LogLevel__Default` | No | Logging level | — | Standard ASP.NET Core logging setting |

### 4. OpenAPI document from `Microsoft.AspNetCore.OpenApi`

- **Use `Microsoft.AspNetCore.OpenApi` (MIT), referenced by the host only — chosen.** The package
  is a NuGet package, not part of the shared framework, so the `OpenHoldemEngine.AspNetCore`
  package itself still ships metadata only, not a document generator (#273).
- There is no Swagger UI, Scalar UI, or any other interactive document viewer in the image, because
  that would require adding one more NuGet package and one more row to the notices for a feature
  the API does not need.

### 5. Dockerfile structure

- **One `Dockerfile` at the repository root — chosen.** The file has three stages: `builder`
  (based on `mcr.microsoft.com/dotnet/sdk:10.0`), and `final` (based on
  `mcr.microsoft.com/dotnet/aspnet:10.0`). Three build targets (`postgres`, `mssql`, and
  `sqlite`) each build `FROM final` and differ only in their environment defaults. `sqlite` comes
  last, so a build without `--target` produces it.
- The base images are:
  - `mcr.microsoft.com/dotnet/sdk:10.0` ships SDK 10.0.401. `global.json` pins 10.0.100 with
    `rollForward: latestMinor`, and it accepts that SDK.
  - `mcr.microsoft.com/dotnet/aspnet:10.0` is Ubuntu 24.04.5 LTS (not Debian), about 340 MB, with
    runtime 10.0.12 (the same patch as the EF Core packages). It has ICU, a non-root user `app`
    (`APP_UID=1654`), and **neither `curl` nor `wget`**.
- The process runs as the `app` user (from the base image).
- **Not used:** `-noble-chiseled-extra` (no shell to debug with) and `-alpine` (needs ICU added by
  hand since SqlClient needs it, and risks the SQLite native binary on musl).

### 6. No authentication in the image

- **No API authentication — chosen.** The API is the trusted game-master API of ADR-0004, which
  returns every hole card. The image is for a trusted network or for running behind a reverse
  proxy that authenticates.
- Rejected: an API key from the environment. It is a security feature whose design (rotation,
  scopes) is not this milestone's.

### 7. No registry

- **Images are built locally — chosen.** Users build images with `docker build --target …`.
- Publishing images to a registry is out of scope and is not planned in this milestone.

### 8. No `HEALTHCHECK` instruction

- **No `HEALTHCHECK` instruction in the Dockerfile — chosen.** The base image has no `curl` or
  `wget`, and adding one just to support a probe would enlarge the image. Orchestrators can probe
  `/health` from outside the container.

## Consequences

- **A host project with its own tests.** It is not a package, so it has no API baseline, no README
  package metadata, and no row in `THIRD-PARTY-NOTICES.md`.
- **All three provider packages are referenced.** The host's project file references
  `OpenHoldemEngine.Sqlite`, `OpenHoldemEngine.PostgreSql`, and `OpenHoldemEngine.SqlServer`.
- **One runtime binary supports all three providers.** The builder stage compiles against all three,
  and the runtime environment picks one at start-up.
- **Docker is not a hard requirement.** Users can also build and run the host locally for testing.
