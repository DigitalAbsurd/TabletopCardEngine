---
status: Accepted
date: 2026-09-30
---

# ADR-0003: Database provider licences

## Context

The v1.0 milestone ships three database provider packages for .NET EF Core:
`TabletopCardEngine.PostgreSql`, `TabletopCardEngine.SqlServer`, and `TabletopCardEngine.Sqlite`.
The engine's own packages reference them, and they in turn reference upstream packages whose
licences were checked at planning time against each package's nuspec on NuGet.org.

Two of these transitive packages declare licences outside the established permissive allowlist
(MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, MS-.NET-Library):

| Package (version at planning time) | Reached through | Licence |
|---|---|---|
| `Npgsql` 9.0.3, `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 | `TabletopCardEngine.PostgreSql`, directly | PostgreSQL |
| `Microsoft.Data.SqlClient.SNI.runtime` 5.1.x | `Microsoft.EntityFrameworkCore.SqlServer` 9.0.x → `Microsoft.Data.SqlClient` 5.1.x (its `net6.0` group), transitively | `file:LICENSE.txt` (Microsoft proprietary, object code distributable) |

All other packages in the providers' closures carry MIT, Apache-2.0, or BSD-2/3-Clause licences.

## Decision

### PostgreSQL Licence → allowlist admission

The PostgreSQL License is OSI-approved, and in substance, a permission-and-notice licence
equivalent to MIT and BSD, stating rights to use, copy, modify and distribute the software and
its documentation provided the permission and copyright notice are retained. It is accepted as a
permissive licence, equivalent to the existing allowlist members.

The allowlist now includes `"PostgreSQL"` for SPDX expressions, with a one-line comment noting
it is OSI-approved and MIT/BSD-equivalent.

### Microsoft.Data.SqlClient.SNI.runtime → single-package exception

The SNI runtime is native code for Windows, which `Microsoft.Data.SqlClient` loads on Windows only
at runtime. The driver's `net6.0` reference group cites it as a file licence, marking it as
proprietary. The exception is granted on the grounds that:

- The licence permits redistribution of the object code in applications.
- The engine's own packages redistribute nothing: consumers restore them from NuGet, and the native
  code stays in the NuGet package cache.
- The exception is narrowly scoped to exactly one package id, so no other `file:` licence or
  proprietary licence can slip through under a wider allowlist.

The exception is a map of accepted package ids to their licence strings, rather than a wider
allowlist entry. Any new `file:` licence or proprietary licence still requires a legal review
before it is added to the map.

## Consequences

- The allowlist in `ThirdPartyNoticesTests.cs` and `scripts/audit-licenses.ps1` each gain one entry
  for `"PostgreSQL"`, with a comment pointing here.
- Both files add a map of accepted per-package licences, currently holding exactly one entry:
  `Microsoft.Data.SqlClient.SNI.runtime` → `file:LICENSE.txt`, with a comment pointing here.
- The test accepts a licence when it is on the allowlist, or when the package is not referenced
  directly and its id is in the exception map with exactly that licence.
- The script accepts a package's licence when it is on the allowlist, or when the package is not
  referenced directly and its id is in the exception map with exactly that licence.
- New exceptions are not added to the map by code review; they require the decision to be reversed
  in a new ADR, and the allowlist and map to be updated together.
