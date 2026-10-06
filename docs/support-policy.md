# Support and deprecation policy

## What this document is

This document defines the supported platforms, dependencies, and versions of OpenHoldemEngine, and the process for deprecating public API members. For versioning policy and how versions relate to the schema, see [docs/versioning.md](versioning.md). For how and when releases are cut, see [docs/release-process.md](release-process.md).

## Target framework

- Every package targets `net10.0`, the .NET 10 LTS release, supported by Microsoft until 2028-11-14.
- Packages do not multi-target.
- **Adding a target framework** is a minor release.
- **Dropping a target framework** is a major release.

## Dependencies

- EF Core and its providers stay on the 10.x line for all releases of 1.x: `Microsoft.EntityFrameworkCore.*` 10.0.x and `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.x.
- Patch updates are taken in patch or minor releases.
- Moving to EF Core 11 or later is not part of 1.x: it requires a newer target framework and is a major release.
- The OpenTelemetry API package (`OpenTelemetry.Api`) follows its own stable 1.x line.

## The next LTS

- .NET 12, the next LTS, is expected in November 2027.
- The move to support it is planned so that a release supporting it ships well before `net10.0` leaves support on 2028-11-14: no later than May 2028.
- Whether it adds `net12.0` next to `net10.0` (a minor release) or replaces it (a major release) will be decided then, in an ADR.

## Supported versions

- The latest minor of the current major is supported.
- A fix for an older minor is made only for a security issue or data loss, on a `release/X.Y` branch (see [docs/release-process.md](release-process.md)).
- Pre-releases (`-preview.N`, `-rc.N`) are not supported once a newer pre-release or the stable version exists.

## Deprecation

- A public member or type is deprecated with `[Obsolete("<what to use instead>", DiagnosticId = "OHE0001")]`, with one sequential `OHExxxx` ID per deprecation.
- The ID is listed in the "Deprecated IDs" section below.
- The deprecated member stays for at least one minor release, and is removed only in the next major release.
- The release that deprecates it mentions "deprecate" in the commit body (see [`docs/task-run.md`](https://github.com/miekhra/OpenHoldemEngine/blob/main/docs/task-run.md#git-and-pull-request-protocol), Git and pull request protocol).
- Log events, span tags and metric instruments (defined in `docs/observability.md`) follow the same rule: documented as deprecated for a minor release, removed in a major release.

## Deprecated IDs

Currently no IDs are in use.
