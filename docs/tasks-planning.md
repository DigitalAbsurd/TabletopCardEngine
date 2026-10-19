# Turning requirements into issues

## What this document is

This is the planning half of the workflow. It describes how a prompt full of requirements or defect
reports becomes a batch of GitHub issues that an AI assistant can implement unattended, one at a
time, through `run-next-task.ps1`.

Its counterpart is [docs/task-run.md](task-run.md), the execution contract: gates, what "done"
means, the git protocol, the repository's traps and the engine facts. **Every issue must link
there**, because it is the only context the implementer gets besides the issue body and the
repository itself.

This document holds no phase history and no list of issues. Those live in GitHub, which is the only
copy anybody reads, and repeating them here just crowds out the parts that are still true.

## The shape of the work

```
prompt with requirements
   → establish the facts          (measure; do not infer)
   → split by root cause          (one cause, one issue)
   → size each issue              (7b / 35b / 70b — and re-split what is too big)
   → order and declare dependencies
   → write each issue standalone
   → check and vet the batch      (label each issue agent-ready last)
   → hand the batch to the runner
```

Each step is below. The steps are not optional and they are not reorderable: sizing an issue before
its cause is established produces a confident number about the wrong task.

## Standing condition: all checks and gates run locally

By design, no workflow in `.github/workflows/` runs on a pull request or a push to `main`. CI runs
only when a release tag is pushed (`release-process.md`). Every check and gate in
[docs/task-run.md](task-run.md#gates) runs **locally, before the push**: build, tests,
`dotnet format --verify-no-changes`, the pack and public API baseline check, the Playwright
browser gate (`TCE_TEST_BROWSER_GATE=1`), the provider matrix (`TCE_TEST_CONTAINERS=1`,
needs Docker), and the documentation site build (needs Docker). The evidence for a gate is the local run, quoted in the pull request. The
`browser-gate-local` label is retired. Do not add a `pull_request` or `push` trigger without being
told to. The workflows are `Release` (`release.yml`, the only one a pushed `v*` tag triggers) and the four it calls,
each of which can also be run by hand for an existing tag (`workflow_dispatch` with a `tag` input, see
`release-process.md`): `Create GitHub Release` (`create-release.yml`), `Deploy demo to GitHub Pages`
(`deploy-demo.yml`, stable releases only), `Publish to nuget.org` (`publish-nuget.yml`) and `Publish to
Docker Hub` (`publish-docker.yml`); the last two are placeholders gated by `TCE_PUBLISH`. `gh workflow list --all` shows
`Deploy demo to GitHub Pages` as `disabled_manually`, and its job skips itself while the repository is
private; it is enabled again when the repository goes public. The old `CI` workflow is gone.
The `Release` workflow is enabled and runs only on a pushed `v*` tag; each run spends the account's Actions minutes.

## 1. Establish the facts before writing a word

A requirement arrives as a symptom — "the page is frozen", "the dropdown is empty", "the deploy
fails". The symptom is not the task. **Reproduce it against the running product and measure the
cause**, then write issues against what you measured.

Concretely, that means: build and run the thing, drive it, and pull real numbers out of it — a
hit-test at the button's centre, a computed `grid-template-rows`, a `scrollLeft` before and after,
the failing workflow step and its actual error, the reflected parameter list of the component you
are about to add an attribute to. Read the source too, but the source tells you what *should*
happen; only the running product tells you what does.

This is not process ceremony. The most recent batch here began as three reported symptoms and one
of them, on measurement, turned out to be **four independent defects in different files**, none of
which was the one the source reading suggested. Issues written from the source reading would have
sent four implementers to four wrong files and closed with the page still frozen.

Two things fall out of measuring, and both belong in the issue:

- **The before-value.** An implementer who has a number to beat can tell a real fix from a
  plausible-looking rewrite. One who has "make it not overflow" cannot.
- **The traps.** Reflecting over `AntDesign.dll` before proposing a markup change is what turned a
  dead dropdown into a two-line fix instead of a hard `InvalidOperationException` at render time.
  If checking cost you an hour, writing the answer into the issue costs the next person nothing.

Record verified **non**-findings too, in the issue that would otherwise attract them: "the
stylesheet is absent from `index.html` on purpose, the package injects it — do not add it". A
disproved theory is worth as much as a proved one, and without it the implementer re-derives it.

## 2. Split by root cause, not by symptom

One symptom can have several causes; one cause can produce several symptoms. **The unit of an issue
is a cause.** A frozen page that turns out to be an invisible overlay, a hijacked scroll container,
an overflowing grid and a squeezed row is four issues, not one — because each has its own file, its
own fix, its own measurement and its own way of going wrong.

Splitting this way is what makes issues small enough to be `7b` and verifiable by a single
measurement. It also means a failure is contained: one wrong fix does not take the other three down
with it.

Two additions to the batch that are easy to forget:

- **File the gap, not only the instance.** If a whole batch of defects passed every gate the
  repository has, one issue in the batch should close that hole — a new gate, not another fix. It
  will be the largest and least `7b` issue in the batch, and it belongs at the end, where the fixes
  it asserts against have already landed.
- **Do not fold a discovered problem into a neighbouring issue.** Something else being broken in a
  file you happened to open is its own issue. The implementer of the neighbouring issue is told not
  to widen scope; give them somewhere to put it.

## 3. Size every issue, and use the size as a test

Every issue carries exactly one of these labels. They say which model the runner should hand the
task to.

| Label | Model tier | The issue is this size when… |
|---|---|---|
| `7b` | Haiku-class | The cause is established, the issue names every file (roughly one to three), the fix is described concretely enough that implementing it is transcription plus care, and verification is either a gate that already exists or a probe written out verbatim in the body. **No design decision is left open.** |
| `35b` | Sonnet-class | The outcome is stated but the approach is the implementer's to pick: more than about three files, a new component or service, a trade-off deliberately left open ("trim the list or refuse the change — say which and why"), or a new test whose shape the issue does not spell out. |
| `70b` | Opus-class | Either the answer is not known yet — diagnosis is part of the task — or getting it wrong is expensive: new infrastructure (a project, a toolchain, CI wiring), a change to the engine's rules or public surface, or a deliverable that is worse than nothing when botched, such as a gate that would certify breakage. |

The runner turns a label into an actual model through `agent.<name>.json`, the assistant-specific
settings file in the repository root — `agent.claude.json.example` is the committed template, and
`agent.*.json` is git-ignored. Which model each tier resolves to is that file's business; an issue
only has to carry the right label.

**Most of a batch must be `7b`.** That is not a hope about the work; it is a constraint on the
decomposition, and the label is how you test it. If more than about a third of a batch sits above
`7b`, the batch is decomposed too coarsely. Go back and split:

- A `35b` usually splits into a `7b` that makes the change and a `7b` that verifies it, or into two
  `7b`s along a file boundary.
- A `70b` whose problem is *unknown cause* splits into a `70b` that only diagnoses — measure,
  report, change no code — and several `7b`s that fix what it found. This is step 1 turned into
  issues when you cannot do it yourself up front.
- A `70b` whose problem is *risk* usually does not split, and should not be forced to. Label it
  `70b`, say plainly in the body that it is the riskiest task in the batch, and tell the
  implementer to leave the pull request unmerged rather than weaken it into passing.

An issue that resists being made `7b` is telling you something true about the task. Listen to it
before overriding it.

## 4. Order the batch and declare dependencies

The runner takes the **lowest-numbered vetted open issue whose dependencies are all closed**
(vetting is step 7). So two
mechanisms decide order, and they do different jobs:

- **Creation order** carries the preference. Create a batch in one pass, in the order it should run,
  and the runner will follow that order on its own.
- **`Depends on:`** carries the constraint — the cases where running out of order would actually
  break. Declare one when the later issue's Definition of done cannot be checked until the earlier
  one has landed, when both touch the same lines, or when the earlier fix is what makes the later
  one reachable at all.

The line the runner parses looks exactly like this, at the start of a line in the issue body:

```
**Depends on:** #61, #62
```

Every `#N` on that line must be `CLOSED` before the issue is picked up. No line means no
dependencies, which means always ready.

Its failure modes are quiet, so get it right the first time:

- **A number that does not exist, or belongs to another repository, is not an error.** The runner
  reads it as not-closed and skips that issue **forever** — it never goes red, the task simply
  never runs. It prints the unmet numbers and their states (`MISSING` for one it cannot find) only
  once *nothing at all* is ready; while other issues are still runnable, a broken reference is
  invisible. Paste real numbers, and only after the issues they refer to exist.
- **Only the first matching line is read.** Put every dependency on that one line.
- **A cycle silently empties the queue.** The runner finds nothing ready and reports the wait
  chain, which at a glance reads much like "the batch is finished".
- A dependency **may** point outside the milestone. The runner resolves any number it did not
  already fetch by looking the issue up directly, so a scoped run still sees it.
- Do not declare a dependency to express mere preference. Creation order already does that, and
  every extra edge is another number that can be wrong.

Prefer a chain to a graph. If the batch's dependency structure needs a diagram to follow, the
issues are probably carved along the wrong lines.

## 5. Milestones

**Attach a milestone only when the prompt that asked for the work names one explicitly.** Otherwise
leave the issue without one.

The runner takes `-Milestone` as an optional filter: given one, it picks only from that milestone;
given none, it picks from the whole repository. A milestone nobody asked for therefore does not
organise anything — it only hides the issue from a scoped run, and it adds a segment to the branch
name (`feature/mls-7/issue-N` rather than `feature/issue-N`) for no reason.

**The exception is `Backlog`.** Every follow-up an implementer files while working a task goes into
the `Backlog` milestone ([docs/task-run.md](task-run.md#where-the-task-text-comes-from)), so the pool
of unplanned work stays in one place instead of scattered unassigned across the repository. When
you plan a batch, triage `Backlog` too: move what belongs to a real milestone there, and bring the
rest to ready where it is.

## 6. Write the issue so it stands alone

The issue **is** the task. Nothing outside it and [docs/task-run.md](task-run.md) will be read.
Write every body to this shape:

```
Part of <milestone, if any>. The execution contract — gates, Definition-of-done discipline, the
git protocol, the repository's traps and the engine facts — is docs/task-run.md. Read it first.

**Depends on:** #N        <- only when there is a real constraint

**Why.** The symptom, the cause, and the measurement that proves it is the cause.
**Scope.** What to change. Bullets, imperative.
**Files.** Every file to touch, by path.
**Definition of done.** The gates, plus the probe with its before-value quoted.
**Notes for the implementer.** The named wrong fixes, and the traps.
**Open decisions.** Forks with their options   <- only while undecided; not ready until gone
### Context for an AI assistant
```

What each part is for, and where each one has gone wrong before:

- **Why** carries the measurement, not the story. "`.table-seats` reports a min-content width of
  1020px, which pins the `2fr` column at 1084px and overflows the page by 328px" is a cause.
  "The table is too wide" is a symptom restated.
- **Files** is a real list. An issue that says "the relevant CSS" is not `7b`, whatever its label.
- **Definition of done** must be checkable by someone who was not in the conversation. Where the
  gates cannot settle it — see fact 21 in the execution contract — paste the exact console probe and
  quote what it printed **before** the fix. "It looks right in the browser" is not a Definition of
  done, and neither is a test that asserts on the text of a stylesheet.
- **Notes for the implementer** is where you forbid the wrong fixes **by name**. This matters more
  than it sounds: the cheap fix for a layout defect is almost always the one that hides it —
  `overflow: hidden`, `pointer-events: none`, a `z-index`, a narrowed assertion — and a small model
  reaches for it unless it is ruled out explicitly. Say "do not fix this with `overflow-x: hidden`;
  it converts a visible bug into an invisible one", not "please fix it properly".
- **Context for an AI assistant** is the orientation an implementer needs that is specific to this
  task: the component chain it sits in, which service calls what, the shape of the markup, the
  subset of engine facts it can trip over. Everything repository-wide belongs in the execution
  contract, not here — if you are writing the encoding rules or the analyzer settings into an issue
  body, they are already in `docs/task-run.md` and you are diluting the part that is new.

Labels: the size label (`7b`, `35b`, `70b`) is required, plus whatever topic labels the repository
uses (`demo`, `blazor`, `phh`, `ci`, `docs`, `observability`, `persistence`, `engine`,
`aspnetcore`, `docker`, `v2`, `feature`). `v2` marks post-1.0 roadmap epics; `feature` marks new
capabilities. Issue titles stay plain imperative English; the implementer turns the title into a
Conventional Commit for the pull request. Do **not** add `agent-ready` while you are still writing.
It is the vetting mark, and it goes on last (step 7).

Some work cannot be done by an assistant at all: a step on nuget.org, Docker Hub or Read the Docs,
a repository setting, pushing a release tag. File it as an issue all the same, so the batch shows the
whole path, and label it `manual-work` instead of a size label. A `manual-work` issue never gets
`agent-ready`, so the runner never picks it; an agent issue that needs it done first names it on its
`**Depends on:**` line like any other issue, and waits until the maintainer closes it. Write its body
to the same shape, with **Steps** — the exact clicks, commands and values — in place of **Files**.

## 7. Check the batch before handing it over

The runner has no judgement and there is no human review step, so the batch is the last place a
mistake can be caught cheaply.

1. `gh issue list --state open` shows exactly the issues you meant to create, contiguous, in the
   order you meant them to run.
2. Every issue has exactly one size label, or `manual-work` and no size label.
3. Every `Depends on:` number exists, is in this repository, and is not part of a cycle. Walk the
   chain by hand once — the runner will not tell you.
4. Read one issue as if you had never seen the conversation. If you have to guess at anything, the
   implementer will guess differently.
5. The batch is mostly `7b`. If it is not, step 3 is not finished.
6. The working tree is clean and pushed. The runner does `git reset --hard origin/main` before every
   task and refuses to start dirty, so an uncommitted change is a discarded change.
7. **Vet every issue, then label it `agent-ready`.** The runner picks nothing without that label.
   Read each body as the instructions it is about to become, including every command, link and
   pasted snippet. That matters most for an issue you did not write yourself. Apply the label only
   when you would run the text yourself, and apply it **after the last edit**.

### The vetting guard

The agent runs with its permission prompts off and merges to `main` by itself, so an issue body is
code that runs on the maintainer's machine. On a public repository anyone can open an issue.
`run-next-task.ps1` therefore picks an open issue only when **all** of the following hold:

| Check | Why |
|---|---|
| It carries `agent-ready` (`-ReadyLabel` changes the name). | Only accounts with triage rights or higher can apply labels, so an outsider cannot vet their own issue. |
| The latest application of that label was by an account with `triage`, `maintain`, `write` or `admin` on the repository. | Rules out a bot or a workflow that labels issues automatically. |
| Neither the body nor the title was edited at or after that moment. | The author can still edit an issue after it is labelled. |
| When `-TrustedAuthors` is given, the author is one of those logins. | An optional second filter, for runs that should take only the maintainers' own issues. |

The consequences for planning:

- **Editing a labelled issue un-vets it**, whoever makes the edit, you included. Review the new text,
  then remove `agent-ready` and apply it again. Until then the runner skips the issue.
- **The agent gets a snapshot, not the live issue.** The runner hands it the title and body as they
  were at vetting time, in a temporary file, and tells it that comments are not instructions.
  Anything the implementer needs therefore belongs in the body. A clarification left in a comment
  is never read.
- **A missing label is as quiet as a wrong `Depends on:` number.** While other issues are runnable,
  nothing mentions an unlabelled one. Once nothing is ready, the runner lists every skipped issue
  with its reason (`has no 'agent-ready' label`, `was applied by 'x', whose role is 'none'`,
  `was edited after 'agent-ready' was applied`, `waits on #N (OPEN)`).
- **Follow-up issues the agent files arrive in `Backlog`, and by default unvetted.** The prompt
  forbids the agent to touch `agent-ready`, but the agent runs with your `gh` credentials, which
  could. Review those issues like any other before labelling them. An issue with an
  `**Open decisions.**` section is not ready: decide, rewrite the body, then label it.
- **`-AllowAgentVetting` hands that review to the agent.** With the switch, the prompt allows the
  agent to label a follow-up it filed itself, provided the issue has no open decisions. The guard
  then passes, because the label was applied with your credentials, and the runner takes the issue
  next with no human having read it. That includes an issue a prompt injection in a comment or a
  linked page talked the agent into filing. Use the switch only for runs you watch, on a repository
  whose issues and comments come from people you trust.

Then:

```powershell
./run-next-task.ps1 -TaskCount 0
```

Add `-TrustedAuthors <login>, <login>` to take only issues opened by those accounts.

`-TaskCount 0` works through every ready issue until none is left. It stops at the first task that
fails to close cleanly rather than piling more work on a broken state.

## Anti-patterns

Each of these has cost this project a batch.

- **Writing issues from a source reading.** The cause you can see in the code is often not the cause
  in the running product, and the issue sends the implementer to the wrong file with full confidence.
- **One issue per symptom.** It bundles unrelated causes, so it cannot be `7b`, cannot be verified by
  one measurement, and half-fails.
- **A Definition of done nothing can check.** If neither a gate nor a pasted probe settles it, the
  implementer will decide it is done, and they will not be lying.
- **Leaving the wrong fix un-forbidden.** Anything that makes the symptom invisible will be tried
  first unless it is ruled out by name.
- **Padding the issue with repository-wide rules.** They are in `docs/task-run.md`. Repeating them
  buries the task-specific paragraph that is the only reason the issue exists.
- **Declaring dependencies you do not need.** Every edge is a number that can be wrong, and a wrong
  number removes the issue from the queue permanently, without a word.
