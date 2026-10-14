# Architecture decision records

Each file here records one decision that is expensive to reverse: what was decided, what else was
on the table, and what the decision costs. A record is written once the decision is made and is not
rewritten afterwards. A later decision that reverses it gets a new record and marks the old one
*Superseded by ADR-NNNN*.

Files are named `NNNN-kebab-case-title.md`, numbered in order and never renumbered. The status is
one of *Proposed*, *Accepted*, *Deprecated* or *Superseded by ADR-NNNN*.

| ADR | Title | Status |
| --- | --- | --- |
| [0001](0001-project-license.md) | License the project under Apache-2.0 | Accepted |
| [0002](0002-opentelemetry-instrumentation-package.md) | Ship the OpenTelemetry instrumentation in its own package | Accepted |
| [0003](0003-database-provider-licences.md) | Database provider licences | Accepted |
| [0004](0004-aspnetcore-integration-package.md) | Ship the ASP.NET Core integration as one package, TabletopCardEngine.AspNetCore | Accepted |
| [0005](0005-container-host-and-images.md) | Container host and images | Accepted |
| [0006](0006-documentation-site.md) | Publish the documentation as a MkDocs Material site on Read the Docs | Accepted; the "no generated API pages" clauses of decisions 1 and 3 superseded by [ADR-0009](0009-generated-api-reference.md) |
| [0007](0007-rename-to-tabletopcardengine.md) | Rename to TabletopCardEngine | Accepted; §3 and the "no other" clause of §4 superseded by [ADR-0008](0008-harmonize-the-public-api.md) |
| [0008](0008-harmonize-the-public-api.md) | Harmonize the public API for more than one game | Accepted |
| [0009](0009-generated-api-reference.md) | Publish a generated .NET API reference in the documentation site | Accepted |
