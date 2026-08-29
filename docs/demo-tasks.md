# Blazor WebAssembly demo — shared background for the milestone issues

## Purpose of this document

`OpenHoldemEngine` ships as a class library with no executable. Today the only way to see it work is
to read its tests. The `Blazor demo` milestone builds a public demo, published to GitHub Pages, that
doubles as a teaching aid: a visitor plays a hand, watches the rows the engine writes to its database,
and reads the exact `ApplyHoldemActionAsync` calls that produced them.

**This document is background, not a task list.** Each task lives in its own GitHub issue in the
`Blazor demo` milestone, and each issue is self-contained: an AI assistant implements it from the
issue text plus the repository. What is collected here is the material that would otherwise be
repeated in every issue — the product scope, the target layout, the engine behaviours that are
surprising enough to trip up an implementer, and the browser checks no automated gate can run.

The task text used to live here as well, duplicated into each issue. It no longer does: one copy, in
the issue, is what an implementer reads, and carrying the other twenty-eight alongside it only crowded
out the background that is actually shared.

Some history is worth keeping, because it explains why the checks and facts below are worded the way
they are. Phases A to E shipped as issues #3 to #19 and produced a demo that built, formatted and
tested clean. Every phase since has been a phase of fixes for defects that passed all three of those
gates, and each one uncovered a layer the previous gate could not see:

- **Phase F** (issues #37 to #44) collected what the first browser run found: a duplicated SQL quote,
  a `bool` where JS interop returns a `string`, a missing `@` on a Razor attribute. All legal code.
- **Phase G** (issues #53 to #55) existed because Phase F closed its eight defects and introduced a
  ninth of exactly the same kind — an AntDesign parameter that does not exist, which froze every route
  while all three gates stayed green. Phase G made component *rendering* part of `dotnet test`.
- **Phase H** (issues #59 to #65) exists because the demo still could not be used after that. The
  console was clean and every component rendered; the game page was unusable anyway, because an
  invisible full-screen overlay swallowed every click, a `scrollIntoView` dragged the page off its own
  left edge after each render, and a grid squeezed its rows until the side panels painted over the
  table. Rendering is not the same thing as being usable, and bUnit computes no layout, so none of it
  could fail a test. See fact 23.

The pattern is the point: each phase closed the blind spot it could see and shipped through the next
one. Fact 21 and fact 23 are the two standing statements of what the gates still cannot check.

## Product scope

| Area | Decision |
|---|---|
| UI stack | Blazor WebAssembly (standalone, net8.0) + Ant Design Blazor |
| Engine changes | **`InternalsVisibleTo` only.** The library's public API is not widened, and `Phh/` is not extracted out of the test project. |
| Storage | SQLite `:memory:` behind a kept-alive `SqliteConnection`, the same pattern as `TestEngineHost`. No persistence across reloads; the UI says so explicitly, and `.phh` export is the way to keep a hand. |
| Play mode | Hot-seat (the visitor acts for every seat) plus step-by-step replay of an imported hand history. No bots. |
| Hand histories | Import and replay `.phh` / `.phhs`, and export the current hand or session. |

### Why in-memory SQLite rather than persisted storage

LocalStorage is a string store capped near 5 MB, which a SQLite database file does not fit
comfortably. Persisting the file to IndexedDB or OPFS is possible but needs a JS interop
synchronisation layer that is a project of its own and adds nothing to what the demo teaches. The
demo therefore keeps the database in the tab's memory, warns the visitor that a reload discards it,
and offers `.phh` export as the durable artefact.

## Target layout

```
src/OpenHoldemEngine.Demo/            # Blazor WebAssembly standalone, net8.0
├── Engine/                           # engine hosting: DI, SQLite, the InternalsVisibleTo bridge
├── Services/                         # DemoGameSession, EngineCallJournal, SqliteBrowser
├── Phh/                              # ported reader plus the new writer
├── Components/
│   ├── Table/                        # the poker table
│   ├── Database/                     # the database inspector
│   └── EngineCalls/                  # the call log with syntax highlighting
├── Pages/
└── wwwroot/                          # prism.js and prism.css are vendored, not loaded from a CDN
src/OpenHoldemEngine.Demo.Tests/
docs/demo-tasks.md                    # this document
.github/workflows/                    # ci.yml and deploy-demo.yml
```

## Engine facts every task depends on

These are the behaviours that are surprising, undocumented in the public API, or actively misleading.
Every one of them was verified against the code at `b7c09ea`. Phase H added fact 23 and
Phase I fact 24, both measured against `8a44cbb` — in a browser and in the build tree.

1. **`HoldemDbContext` is `internal` and declares no `DbSet<>`.** An outside consumer cannot call
   `Database.EnsureCreated()`. That is the single reason the demo needs `InternalsVisibleTo`.
2. **`HoldemActionStartGame` and `HoldemActionFinishGame` are public but unwired.**
   `IHoldemActionHandler.CreateHandlerForAction` throws `NotImplementedException` for both. Starting
   a game means `HoldemActionCreateGame` followed by `HoldemActionStartHand`.
3. **`GameData.HandData` is declared non-nullable but is assigned `default!`** by `GameDataMapper`
   while `currentHand.No == 0`, i.e. between `CreateGame` and the first `StartHand`. Null-check it.
4. **`SeatData.BetSize` and `HandResultData.Description` are never populated.** Do not design UI
   around either.
5. **`HandData.Evaluations` mixes two bases on purpose.** A `Call`'s `MinValue`/`MaxValue` is an
   *increment* ("how much more to put in"); a `Raise`'s `MinValue`/`MaxValue` are both an *absolute
   street total* ("raise to"). `HoldemActionPlayerAction.Size` for a raise expects the absolute. A
   degenerate raise window arrives as a synthetic `Raise(allInTo, allInTo)`.
6. **`Pot` excludes the current street.** `HandData.Pot` is prior streets only and `StreetPot` is the
   current street's contributions; a total pot display must add them.
7. **`GameOptionData.AllowRebuy`, `HoldemActionStartHand.BuyIns` and `KickOffSeats` are read by no
   handler.** They are declared and ignored. Do not present them as working controls.
8. **`JoinSeats` must be in ascending `SeatNo` order** — `StartHandActionHandler` walks the array in
   order to resolve the first hand's button.
9. **Seat numbers are 1-based; `0` is the "nobody" sentinel** for `NextPlayerSeatNo`, `DealerSeatNo`
   and the blind seats.
10. **`Street` is not sequential.** `PreFlop = 0, Flop = 3, Turn = 4, River = 5, Showdown = 6,
    Finished = 7` — the middle three encode the community-card count. `StreetExtensions.Next()` is
    `internal`, so the demo needs its own ordering table.
11. **Card strings are `"ah"`** — rank first, lowercase, from `Card.ToString()`. PHH writes `"Ah"`.
12. **Value converters store enums as text.** In the database, `HandAction.Street`,
    `HandAction.ActionType` and `HandPlayerEndState.RankType` are strings, and cards are two-character
    strings.
13. **`HandAction` is the ledger.** `Amount` is a seat's cumulative wager for the street, `AddAmount`
    the increment, `Size` the requested size. Composite primary key `(GameId, HandNo, No, SeatNo)`.
14. **`HoldemEngineOptions.Instance` is a process-wide singleton**, and `AddHoldemEngine` registers
    `HoldemEngine` as a singleton over it. Harmless in a single-user WebAssembly tab, but it means the
    demo cannot host two differently-configured engines side by side — including one for play and one
    for replay. Replay reconfigures the same engine.
15. **`ICardShuffler.GetDeckCardsAsync` is called again before every street change**, so an
    implementation must be idempotent within a hand.
16. **Only three validation failures exist**, all from `Resources/PackResource.resx`: the game id
    already exists, the current hand is completed, the previous hand is not completed.
17. **`Directory.Build.props` sets `TreatWarningsAsErrors` and `AnalysisMode=Recommended`
    repository-wide**, with an empty `WarningsNotAsErrors`. Generated Razor code will fail the build
    without relaxations, and per CLAUDE.md those relaxations belong in `.editorconfig` with a
    justifying comment, never in a `NoWarn` in the csproj.
18. **There is no `.github/` directory at all.** CI and deployment are greenfield.

Facts 1 to 18 describe the repository as the first demo task found it. Phase F adds three more, verified against
`fa656c3` — the state after the whole chain had landed — Phase H adds one (fact 23), measured in a
browser against `6c90304`, and Phase G adds one, verified against
`c6aebc9`.

19. **`.github/` exists now**, holding `ci.yml` and `deploy-demo.yml` from the CI and deployment task (issue #17), so fact 18 is
    history rather than current state. `deploy-demo.yml` declares a `permissions:` block, which means
    every scope it does not list is dropped to *none* — the reason issue #37 exists.
20. **`HoldemEngineOptionsBuilder.WithCardShuffler` takes a non-nullable `ICardShuffler`**, so the
    builder cannot express "deal at random again". The engine falls back to a real shuffle only when
    `HoldemEngineOptions.Instance.CardShuffler` is `null`, which the demo can assign directly because
    it has `InternalsVisibleTo`. Read together with fact 14: a shuffler is global state, so it has to
    be uninstalled and not merely emptied.
21. **The automated gates cannot see the browser.** `dotnet build`,
    `dotnet format OpenHoldemEngine.sln --verify-no-changes` and `dotnet test` are the only checks the
    task runner has. A Definition of done that reads "it looks right in the browser" is therefore not
    implementable by the runner, so every Phase F task lands a test, or a grep, that settles it without
    one. The genuinely browser-only checks are collected in
    [End-to-end verification](#end-to-end-verification) for a human to run.
22. **AntDesign has no catch-all for unknown parameters.** Until Phase G the demo test project had
    nine test files and 65 tests, every one of them over a *service*, with no bUnit reference and no
    `.razor` component instantiated anywhere. That mattered more than it sounds, because **AntDesign's
    components have no
    `CaptureUnmatchedValues` catch-all** — the base chain `AntDomComponentBase` to `AntComponentBase`
    exposes only `Class`, `ClassMapper`, `ComponentIdGenerator`, `ConfigProvider`, `Id`, `Ref`, `RTL`,
    `Style`, `IsDisposed`, `Js`, `RefBack` — so an attribute that is not a real `[Parameter]` is an
    `InvalidOperationException` thrown when parameters are set at render time, not a compile error.
    Legal C#, legal Razor, all three gates green, application dead. Phase G closed the *rendering*
    half of that — bUnit is referenced now and components are rendered by tests — but the parameter
    trap itself is permanent: it is how the package is built, and it is still a render-time exception
    rather than a compile error. Check the parameter list before adding an attribute to an AntDesign
    component; reflecting over `AntDesign.dll` beats guessing, and guessing is what put two dead
    parameters into shipped markup.
    <br>Two related traps live in the same package. `ButtonType`, `ButtonShape` and `AlertType` are
    static classes of `string` constants rather than enums, so `Type="primary"` and
    `Type="@ButtonType.Primary"` both bind — neither spelling is wrong. And `Modal`'s `Footer`,
    `OkText` and `CancelText` are `OneOf<string, RenderFragment>?` (AntDesign 0.17.0 depends on `OneOf`
    2.1.155), which a plain string binds to through a compiler-inserted conversion.
23. **The gates cannot see layout.** bUnit, added in Phase G, renders markup and computes **no
    geometry**. Element size, stacking order, hit-testing and scroll position are all invisible to
    `dotnet test`, so a page can be fully rendered, throw nothing, and still be impossible to use.
    Four defects found in Phase H, each measured in a browser on the running demo while all three
    gates were green and the console was empty:
    <br>— a `position: fixed`, full-viewport, `z-index: 1000` container rendered unconditionally with
    no visible child. A 30-point hit-test grid across the page returned that container at **30 of 30
    points**; no control in the demo could be clicked for the whole duration of a game.
    <br>— `scrollIntoView({ block: 'nearest' })` called after *every* render. It takes no `inline`
    argument here, so it defaults to `inline: 'nearest'` and scrolls **both** axes of every scrollable
    ancestor: `scrollLeft` jumped `0 → 316`, putting a button at `x = -268`, where a scroll container
    can never reach and `elementFromPoint` returns `null`.
    <br>— `repeat(auto-fit, minmax(150px, 1fr))`, which cannot collapse its track count while the grid
    is sized under an intrinsic constraint. Six seats reported a min-content width of 1020px, the
    enclosing `2fr` track was pinned at 1084px, and the page overflowed horizontally by 328px.
    <br>— `height: 100%` on a grid, which makes it fit two `auto` rows into a definite height instead
    of growing: rows resolved to `400px 290px` for 763px of content, and with `overflow: visible` the
    surplus spilled under a later sibling that then took the clicks.
    <br>Read together with facts 21 and 22, this is the third layer of the same problem. Facts 21 and
    22 say the gates cannot see the browser and cannot see rendering; this one says that rendering is
    not the same thing as being usable. The honest gate for a layout defect is a real browser
    (issue #65); until that lands, the checks in
    [End-to-end verification](#end-to-end-verification) are the only thing standing behind it, and a
    Definition of done for a layout task must name a measurement, not an impression.
24. **A `:root` in a scoped stylesheet never matches, so the demo's colour palette does not exist.**
    Blazor's scoped-CSS rewriter appends the component's `[b-xxxxxxxxxx]` attribute to *every*
    selector in a `.razor.css` file, `:root` included. `EngineCalls.razor.css` defines eleven
    `--color-*` properties on `:root` inside two `prefers-color-scheme` blocks; the generated
    `obj/Debug/net8.0/scopedcss/.../EngineCalls.razor.rz.scp.css` emits them under
    `:root[b-4jmrgijl9i]`, and the scope attribute is stamped only on elements the component renders
    — `<html>` is not one of them — so not one of those properties is ever set. Compounding it, the
    nine `--ant-*` names used by `DatabaseInspector.razor.css` were invented and defined nowhere:
    AntDesign Blazor 0.17.0 ships a compiled Less stylesheet with hard-coded colours and publishes no
    custom properties under those names.
    <br>The cost is **32 dead declarations**, 16 in each of those two files. A `var()` with no
    fallback and no definition makes the whole declaration invalid at computed-value time, so
    `border: 1px solid var(--ant-border-color)` computes to *no border at all* rather than to a
    default one. Nothing reports this: the build does not analyse CSS, and bUnit computes no styles,
    so it reads as "the stylesheet was never written" rather than as a defect. Define custom
    properties in the global `wwwroot/css/app.css`, never in a `.razor.css` (issue #76).

## A known, deliberate duplication

The PHH tooling (`PhhReader`, `PhhActionParser`, `PhhCards`, `PhhHandDeck`, `PhhAction`, `PhhHand`)
is `internal` inside the *test* project. Since the agreed constraint is `InternalsVisibleTo` only —
no restructuring — the demo gets its own port of these types under `OpenHoldemEngine.Demo.Phh`
(the demo PHH port, issue #13) rather than a shared project.

This is duplicated source, accepted knowingly. The rejected alternative was
`<Compile Include="..\OpenHoldemEngine.Tests\Phh\*.cs" Link="..." />`, which drags a foreign
namespace into the demo assembly (breaking `IDE0130`, an error here) and couples the shipped demo to
a test project. If the duplication becomes a maintenance problem, the follow-up is to extract a
shared `OpenHoldemEngine.Phh` project that both consumers reference; issue #13 leaves a note pointing
at that option.

## Issue conventions

- Labels: `demo` on every issue, plus one of `blazor`, `phh`, `ci` or `docs`.
- Milestone: `Blazor demo`.
- **The issue is the task.** This document holds no task text, so an issue body must stand entirely on
  its own: why the defect exists, what to change, which files, how it is verified, and the subset of
  [Engine facts every task depends on](#engine-facts-every-task-depends-on) that the task can trip
  over. Do not write an issue that only makes sense after reading something else.
- **Issues are created in the order they should be implemented.** `run-next-demo-task.ps1` takes the
  open issues of the milestone in ascending issue number and picks the first one that is ready, so
  creation order *is* execution order. Create a whole phase in one pass, in the order it should run, so the
  numbers land contiguously.
- **No dependency lines.** The runner still parses a `Depends on:` line if it is there, but ordering the
  issues correctly makes it redundant, and a wrong number in it does not fail loudly — it makes the
  runner skip the task forever. Order the work instead of declaring a graph.
- Where a task genuinely cannot be verified by `dotnet build` / `dotnet format --verify-no-changes` /
  `dotnet test` — see facts 21 and 23 — the issue must carry the exact browser probe that settles it,
  with the measured before-values quoted, so the implementer can tell a real fix from a plausible
  rewrite. "It looks right in the browser" is not a Definition of done.

## End-to-end verification

These are the checks no automated gate performs. Run them by hand once a phase closes, and treat a
failure here as a new issue in the `Blazor demo` milestone rather than as a note in a pull request.

1. `dotnet build` and `dotnet test` from the repository root — the analyzer relaxations for the demo
   must not have loosened anything for the library or the tests.
2. `dotnet run --project src/OpenHoldemEngine.Demo`, then in the browser: create a six-handed game,
   play a hand to showdown, and confirm the table, the database inspector and the call log stay
   consistent at every step.
3. In the inspector after that hand: `HandAction` holds rows keyed on `(GameId, HandNo, No, SeatNo)`,
   with `Street` and `ActionType` stored as text and cards as two-character strings.
4. Import the bundled sample, replay a hand step by step, and confirm the final stacks match the
   recorded `finishing_stacks`.
5. Export that hand to `.phh`, re-import the exported file, and confirm the action sequence is
   identical.
6. `dotnet publish -c Release`, then serve `wwwroot` from a sub-path locally, to confirm the
   `base href` rewrite and `404.html` work before relying on GitHub Pages.

### After Phase F

These are the checks that no automated gate can perform, and whose absence is why Phase F was needed
at all. Run them in a browser with the developer console open.

7. Create a game. The database inspector must populate — table list, row counts, rows — with no yellow
   error bar and **no exceptions in the console**. This one check is what the whole of issue #39 exists
   for, and it failed on every run before it.
8. Reload the page with the console open, before touching anything. There must be no `JsonException`
   from a `localStorage` call (issue #40).
9. Play a hand to showdown, watching all three panels stay consistent, then start the next hand.
10. Import a hand history, replay it to the end, then start a **normal** game from the same page. The
    normal game must deal random cards and not fail (issue #41).
11. Read the page for leaked source text: an alert with no colour or icon, a visible `.ToString("F2")`,
    a stray `m` after the big blind, a literal `(#{...})` next to the replayed hand's name (issue #42).
12. Dismiss the walkthrough, reload, and confirm it stays closed. Then reopen it from the header button
    and confirm it starts at step 1 (issue #44).
13. Confirm the deployed site at its Pages URL actually loads, including a deep link, once
    `deploy-demo.yml` has run green on `main` (issue #37).

### After Phase G

Checks 7 to 13 above were written for a human because no gate could run them. Phase G moves the first
and most important of them into `dotnet test`, so what is left here is short — and check 14 is the one
that matters, because it is the check whose absence let a dead application ship twice.

14. Open the demo with the browser console open and touch nothing. The console must be **empty** — no
    `InvalidOperationException`, no `does not have a property matching the name`, and no yellow bar in
    the footer. Both Phase G defects would have failed this in the first second (issue #53).
15. Click the round question button in the header. The walkthrough must open at step 1, every time,
    including after it has already been dismissed once (issues #44 and #53).
16. Walk the walkthrough to Done, reload, and confirm it stays closed. Then open it again from the
    header and close it with the X — the X must dismiss it, not advance it a step (issue #53).
17. Force a failure in one panel — comment out a guard, or point a component at a null — and confirm
    the rest of the page still works and the failing panel shows a message with a Retry that recovers.
    Put the code back afterwards (issue #55).

### After Phase H

Checks 14 to 17 assume that a rendered page is a working page. Phase H is what happened when that
assumption failed: the console was empty, every component rendered, and nothing on the game page could
be clicked. So these checks measure instead of looking. Run each of them at **1500px and 1280px wide**
— the two defects on either side of the 1366px breakpoint would each have hidden from a single width —
and paste the probe into the browser console rather than judging by eye.

18. With a game in progress, no element matching `.hand-result-container` exists in the DOM, and
    `document.elementFromPoint` at the centre of each of the Fold / Check / Call / Raise buttons
    returns that button. Before issue #60 the container covered the whole viewport and captured
    30 of 30 probe points.
19. Note `main.layout-content.scrollLeft` and `scrollTop`, take a game action, and read them again.
    Both must be unchanged. Before issue #61 they jumped to the bottom-right corner after every
    render, and any manual scroll was undone by the next one.
20. `main.layout-content.scrollWidth === main.layout-content.clientWidth` — no horizontal scrollbar on
    the game page. Before issue #62 it overflowed by 328px, which is the space the poker table was
    being pushed into.
21. On `Game Setup`, `.main-region`'s `scrollHeight` equals its `clientHeight` — nothing spills — and
    `document.elementFromPoint` at the centre of `Create and Start Game` returns that button. Before
    issue #63 it returned a paragraph from the panel painted over it, at 1280px only.
22. Open the `Max Players` dropdown: five options, no `No Data`, and the closed control shows the
    current value. Select `2 (Heads-up)`, create the game, and confirm two seats with the button
    posting the small blind and acting first pre-flop (issue #64).
23. Confirm the deployed site at its Pages URL loads, including a deep link, once `deploy-demo.yml`
    has run green on `main` (issue #59). This has never yet been seen to work.
24. Once issue #65 has landed, the browser gate covers checks 18 to 21 automatically. Revert one of
    the four fixes in a working tree and confirm the gate fails — a gate that has never been seen to
    fail has not been shown to work — then put it back.

### After Phase I

Phase H made the game page usable. Phase I is about the panels beside it, and about a class of defect
one step below the last one: CSS that parses, ships and does nothing, because the values it names were
never defined. Fact 24 is the reason checks 25 and 27 measure a *computed* value rather than reading
the stylesheet.

Each of these ends with stopping the dev server. A run is not finished while a listener is still up —
check with `Get-NetTCPConnection -LocalPort 5170,7226 -State Listen -ErrorAction SilentlyContinue`,
which must return nothing (issue #75).

25. Read `getComputedStyle(document.documentElement).getPropertyValue('--ant-border-color')` and the
    same for the other nineteen `--ant-*` / `--color-*` names. Every one must be non-empty. Before
    issue #76 all twenty resolved to `""`, taking 32 declarations down with them silently.
26. Open the Database Inspector's table dropdown: it lists the engine's tables and shows no `No Data`,
    and picking a different table swaps the columns and rows below it (issue #77).
27. On a `.simple-table` cell, no computed border reports `none`, and the header row's
    `backgroundColor` is a visible grey rather than `rgba(0, 0, 0, 0)` (issue #78).
28. Take six or more actions, then read `.engine-calls-list`: `scrollHeight > clientHeight`, the panel
    has stopped growing with each action, and the newest entry is in view. Scroll the list to the top
    by hand, take one more action, and confirm it returns to the bottom. Before issue #79 the list
    never overflowed, so `scrollTop = scrollHeight` was clamped straight back to `0` and the auto-scroll
    — which was already written and already correct — could not do anything.
29. When it is a seat's turn a prominent trigger button is visible and is its own hit target; opening
    it reveals the action controls, applying an action closes it, and with it closed nothing invisible
    is left over the page — `document.scrollingElement` neither scrolls nor overflows sideways
    (issue #80). Play a hand to showdown entirely through the drawer with the console open.
