# ADR-0001: License the project under Apache-2.0

- **Status:** Accepted
- **Date:** 2026-09-29
- **Issue:** [#130](https://github.com/miekhra/OpenHoldemEngine/issues/130), milestone
  `v1.0 · Legal & dependency audit`

## Context

OpenHoldemEngine has been under the MIT License since its first commit. Before the repository goes
public and the library goes to nuget.org, the license should be a deliberate choice rather than a
template default. The milestone names four candidates: MIT, Apache-2.0, LGPL, or AGPL dual-licensed
with a commercial license.

The choice has two requirements:

1. **As open as possible.** Anyone may use, modify and redistribute the engine, commercially
   included, without having to open their own code.
2. **Attribution is mandatory.** Whoever reuses the engine, commercially included, has to credit
   its author.

The dependencies do not narrow the choice. The audit behind
[THIRD-PARTY-NOTICES.md](https://github.com/miekhra/OpenHoldemEngine/blob/main/THIRD-PARTY-NOTICES.md) resolved every package the projects use,
transitive ones included. Each is MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, public domain
(SQLite) or the legacy Microsoft .NET Library License, and the vendored PHH corpus is MIT. None of
these is copyleft, so none of them limits what the project's own code may be licensed under. All of
them can be combined into an Apache-2.0 work. The one GPL-derived dependency, the `HandEvaluator`
package, was already replaced by an in-repo evaluator (#124).

## Options considered

### MIT

The shortest permissive license, and the current one. Its only condition is that the copyright
notice and the license text are "included in all copies or substantial portions of the Software".
That is attribution in a weak form: a redistributor meets it by leaving a LICENSE file inside a
package, and nothing obliges them to show the credit to anyone. MIT has no patent grant.

Meets requirement 1 fully, requirement 2 only nominally.

### Apache License 2.0 — chosen

As permissive as MIT on use, modification, sublicensing and commercial use, and a derivative work
may be closed source. In addition:

- **Section 4(d), NOTICE.** If the work includes a `NOTICE` file, every redistribution of the work
  or of a derivative must carry a readable copy of the attribution notices in it. The copy goes in a
  NOTICE file shipped with the derivative, in its documentation, or in a display the derivative
  generates, wherever such third-party notices normally appear. This is the attribution the second
  requirement asks for, stated as a license condition.
- **Section 4(b).** Modified files must carry a notice that they were changed.
- **Section 3, patent grant.** Every contributor grants a patent license for their contribution.
  The grant terminates for anyone who sues over the work's patents.
- **Section 6, trademarks.** The license grants no right to the project's name, so a fork cannot
  present itself as OpenHoldemEngine.

Meets both requirements.

### LGPL-3.0

Weak copyleft. Anyone may link the library into a closed product, but changes to the library
itself must be published under the LGPL. The product must also let its users relink it against a
modified library. The second condition is awkward for a NuGet library, and it is at odds with how
.NET applications are trimmed and published. It keeps the author's credit, but it is less open than
the dependencies it sits on.

Fails requirement 1.

### AGPL-3.0 with a commercial license

Strong copyleft that also reaches network use. A service built on the engine has to publish its
entire source, or buy a commercial license from the author. It is the only option that enforces
anything against SaaS-only use. It is also the least open option, it deters exactly the reuse
requirement 1 asks for, and selling the commercial side requires the author to hold the rights to
every contribution, which means a CLA.

Fails requirement 1.

## Decision

The project is licensed under the **Apache License, Version 2.0**. The attribution the author
requires lives in [NOTICE](https://github.com/miekhra/OpenHoldemEngine/blob/main/NOTICE) at the repository root. `LICENSE` carries the verbatim
license text, and the README's License section says both.

The relicensing from MIT is possible without anyone else's consent: every commit so far is by the
sole author, who holds the copyright to all of it. Copies obtained under MIT before this change stay
under MIT. That cannot be revoked, and nothing here tries to.

## Consequences

- **Attribution holds for redistribution, not for SaaS-only use.** Apache-2.0's conditions, section
  4(d) included, are triggered by redistributing the work. A service that runs the engine on its own
  servers and never ships it redistributes nothing, so it owes no NOTICE. No permissive license
  closes that gap. Only the AGPL does, and it was rejected above.
- **GPLv2-only projects cannot include the engine.** The Free Software Foundation considers Apache-2.0
  incompatible with GPLv2 (because of the patent termination clause), though compatible with GPLv3.
  That is a limitation for a small class of downstream projects, and it is accepted.
- **`NOTICE` stays short.** It is the project's own attribution only. Third-party notices stay in
  THIRD-PARTY-NOTICES.md: every line of NOTICE is a line each redistributor has to reproduce, so it
  must not grow into an inventory.
- **Dual licensing stays possible, but only while the author holds all the rights.** As the sole
  copyright holder the author may also sell the engine under other terms. Once outside contributions
  are accepted, doing so needs a CLA, or at least a DCO plus an inbound license. That choice belongs
  to the monetization item of this milestone.
- **Package metadata follows later.** The NuGet milestone sets `PackageLicenseExpression` to
  `Apache-2.0` in `Directory.Build.props` and packs `NOTICE` into the package beside the README.
