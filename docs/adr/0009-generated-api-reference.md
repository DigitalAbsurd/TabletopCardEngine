# ADR-0009: Publish a generated .NET API reference in the documentation site

- **Status:** Accepted
- **Date:** 2026-10-14
- **Issue:** [#438](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/438), milestone
  `v1.0 · README & community docs`

## Context

The documentation site has no API reference. ADR-0006 forbids one (decision 1 and decision 3: "no
generated API pages"), reasoning that the API reference stays with the XML doc comments and
IntelliSense. For a class library whose whole surface is `IHoldemTable.ApplyAsync` plus DTOs, the
reference is the most important part of the documentation. The maintainer has reversed that decision.

Measurements were taken during planning against the Release build of the seven packages in
`TabletopCardEngine.Packages.slnf`, in a scratch copy of the site, with `squidfunk/mkdocs-material:9.7.7`:

- `DefaultDocumentation.Console` 1.2.6 (published 2026-09-30, license `MIT-0`, repository
  `Doraku/DefaultDocumentation`) generates 70 pages for the seven assemblies (core 45, AspNetCore 10,
  the other five 3 each) with `GeneratedAccessModifiers: Api` and `GeneratedPages: Assembly,Namespaces,Types`.
  No internal type appears.
- Two runs are byte-identical, and a Debug build gives the same output as a Release build. All seven
  runs take about 8 seconds.
- The definitions are complete: default values (`decimal size=0m`), `init` accessors, nullability
  (`HandData? HandData`), `protected` constructors. Every one of the 106 member and type names in the
  hand-run inventory of `docs/public-api.md` appears in the generated pages.
- With every generated page in the `nav`, `mkdocs build --strict` reports 0 warnings. With only the
  package pages in the `nav`, it fails on `validation.omitted_files`: the `nav` must list every page.
- Cross-assembly `cref`s (AspNetCore → core) resolve through a links file the core run writes.

## Decision

### 1. The site carries a generated API reference

The site publishes a generated API reference of the public and protected surface of the seven
packages in `TabletopCardEngine.Packages.slnf`. `TabletopCardEngine.Server` and the demo are not
packages and are not documented. The REST API is not part of it: `docs/aspnetcore.md` and the
server's `/openapi/v1.json` stay its reference, and no OpenAPI plugin is added.

### 2. Generator: `DefaultDocumentation.Console` 1.2.6, as a local dotnet tool

The generator is installed in `.config/dotnet-tools.json` as a local dotnet tool. Its license is
`MIT-0`, which is outside the package allowlist of `ThirdPartyNoticesTests` (MIT, Apache-2.0,
BSD-2/3). It is accepted by the maintainer because it is a development tool like `dotnet-ef`:
nothing of it ships or is redistributed, the generated pages are made from the project's own XML doc
comments, and `MIT-0` asks for less than `MIT` (no attribution). The allowlist is not widened.

Alternatives considered: DocFX (its own site generator, not Markdown for MkDocs), extending
`scripts/list-public-api.ps1` (more in-house code to maintain for the same output), an OpenAPI
plugin (documents HTTP endpoints, not the .NET surface).

### 3. The generated Markdown is committed

The generated Markdown is committed under `docs/api/<Package>/`, and the `nav` entries for it are
generated into `mkdocs.yml`. Read the Docs and the Docker-based docs gate need no .NET SDK, and the
pages read on GitHub before launch (ADR-0006, decision 6). A test fails when the committed pages
differ from what the built assemblies produce.

### 4. The MkDocs toolchain pin is unchanged

ADR-0006, decision 5: no MkDocs plugin is added for the reference.

## Consequences

- **The next issues of the milestone implement this record.** Later issues add the tool, a script,
  `docs/api/` and the `nav` entries, and cite this record.
- **Development builds gain a new tool.** The tooling is installed and cached locally on first use,
  much like `dotnet-ef` or `dotnet-ef dbcontext optimize`.

