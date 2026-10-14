# ADR-0006: Publish the documentation as a MkDocs Material site on Read the Docs

- **Status:** Accepted; the "no generated API pages" clauses of decisions 1 and 3 superseded by [ADR-0009](0009-generated-api-reference.md)
- **Date:** 2026-10-06
- **Issue:** [#359](https://github.com/DigitalAbsurd/TabletopCardEngine/issues/359), milestone
  `v1.0 · README & community docs`

## Context

The project's scope and design are now mature enough to warrant a documentation site. The milestone
commits to publishing one, and this record fixes the decisions the milestone's later issues build on,
so that each of them does not reopen them.

## Decision

### 1. The README is the entry point; everything beyond it lives in a MkDocs Material site on Read the Docs

Three options were on the table:

- **MkDocs Material hosted on Read the Docs — chosen.** Read the Docs is free for public
  repositories, builds a version per tag, and serves `stable` (the latest tag) next to `latest`
  (`main`). The README keeps the pitch, the limits, the quick start and links; it does not grow
  into a manual.
- GitHub Pages with Jekyll. Pages would share the root of `https://digitalabsurd.github.io/TabletopCardEngine/`
  with the Blazor demo, which is deployed only for stable tags. Documentation there would compete
  for the root and would have no version for pre-releases or `main`.
- Sphinx or DocFX. The content is Markdown that already exists, and neither tool adds anything
  for it. The API reference stays with the XML doc comments and IntelliSense; there is no need
  for generated API pages.

### 2. Not GitHub Pages

The Blazor demo occupies the root of `https://digitalabsurd.github.io/TabletopCardEngine/` and is deployed
only for stable tags (`deploy-demo.yml`, called by `release.yml`). Documentation there would
compete for the root and would have no version for pre-releases or `main`.

### 3. Not DocFX, not Sphinx

The content is Markdown that already exists, and neither tool adds anything for it. The API
reference stays with the XML doc comments (`GenerateDocumentationFile` is on and `CS1591` is an
error) and IntelliSense: no generated API pages.

### 4. The contributor and agent workflow is excluded from the site

`docs/task-run.md` and `docs/tasks-planning.md` stay where they are, because every issue links
`docs/task-run.md` by that path, and `mkdocs.yml` lists them in `exclude_docs`. A published page
that refers to them links their GitHub URL.

### 5. The toolchain is pinned

- **`mkdocs==1.6.1` and `mkdocs-material==9.7.7` (plus `pymdown-extensions==11.0.1`) — chosen.**
  These versions go into `docs/requirements.txt` for Read the Docs, and the `squidfunk/mkdocs-material:9.7.7`
  image is used for the local gate. Both carry exactly these versions: `pip list` inside the image
  prints `mkdocs 1.6.1`, `mkdocs-material 9.7.7`, `pymdown-extensions 11.0.1`. Every build in
  that image prints a warning from the Material team that MkDocs 2.0 removes the plugin system
  and rewrites theming with no migration path, so the pin is deliberate and moving off it is a
  decision of its own.

### 6. Until the repository is public, links into the site point at GitHub

- **Links point at `docs/` on GitHub** (`https://github.com/DigitalAbsurd/TabletopCardEngine/blob/main/docs/...`),
  from the README and from the package READMEs alike. The milestone `v1.0 · NuGet packages & public launch`
  connects Read the Docs and switches those links.

## Consequences

- **Pages must read correctly on two renderers until launch.** GitHub renders Markdown, and MkDocs
  renders it differently. A link that leaves `docs/` must be an absolute GitHub URL, because MkDocs
  cannot resolve it.
- **The gate gains a Docker-based docs build.** A local build step verifies the MkDocs build does
  not emit warnings and the resulting site is well-formed.
- **The site is not live until the launch milestone.** Until the repository is public and Read the
  Docs is connected, the site is built but not served.
