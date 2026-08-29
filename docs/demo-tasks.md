# Blazor WebAssembly demo — shared background for the milestone issues

## Purpose of this document

`OpenHoldemEngine` ships as a class library with no executable. Today the only way to see it work is
to read its tests. The `Blazor demo` milestone builds a public demo, published to GitHub Pages, that
doubles as a teaching aid: a visitor plays a hand, watches the rows the engine writes to its database,
and reads the exact `ApplyHoldemActionAsync` calls that produced them.

**This document is background, not a task list, and it is kept short on purpose.** Each task lives in
its own GitHub issue and is self-contained: an AI assistant implements it from the issue text plus the
repository. What is collected here is only the material that would otherwise be repeated in every
issue — the product scope, the target layout, the engine behaviours surprising enough to trip up an
implementer, and the browser checks no automated gate can run. Everything else, including the story of
how a defect was found and what it measured before the fix, belongs in the issue that fixes it.

## Product scope

| Area | Decision |
|---|---|
| UI stack | Blazor WebAssembly (standalone, net8.0) + Ant Design Blazor |
| Engine changes | **`InternalsVisibleTo` only.** The library's public API is not widened, and `Phh/` is not extracted out of the test project. |
| Storage | SQLite `:memory:` behind a kept-alive `SqliteConnection`, the same pattern as `TestEngineHost`. No persistence across reloads; the UI says so explicitly, and `.phh` export is the way to keep a hand. |
| Play mode | Hot-seat (the visitor acts for every seat) plus step-by-step replay of an imported hand history. No bots. |
| Hand histories | Import and replay `.phh` / `.phhs`, and export the current hand or session. |

LocalStorage is a string store capped near 5 MB, which a SQLite database file does not fit
comfortably, and persisting to IndexedDB or OPFS needs a JS interop synchronisation layer that is a
project of its own. That is why the database lives in the tab's memory, the UI warns that a reload
discards it, and `.phh` export is the durable artefact.

## Target layout

```
src/OpenHoldemEngine.Demo/              # Blazor WebAssembly standalone, net8.0
├── Engine/                             # engine hosting: DI, SQLite, the InternalsVisibleTo bridge
├── Services/                           # DemoGameSession, EngineCallJournal, SqliteBrowser
├── Phh/                                # ported reader plus the new writer
├── Components/
│   ├── Table/                          # the poker table
│   ├── Database/                       # the database inspector
│   └── EngineCalls/                    # the call log with syntax highlighting
├── Pages/
└── wwwroot/                            # prism.js and prism.css are vendored, not from a CDN
src/OpenHoldemEngine.Demo.Tests/        # bUnit component render tests plus service tests
src/OpenHoldemEngine.Demo.BrowserTests/ # Playwright layout gate, behind OHE_TEST_BROWSER_GATE
docs/demo-tasks.md                      # this document
.github/workflows/                      # ci.yml and deploy-demo.yml
```

## Engine facts every task depends on

These are the behaviours that are surprising, undocumented in the public API, or actively misleading.

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
16. **`HoldemEngineOptionsBuilder.WithCardShuffler` takes a non-nullable `ICardShuffler`**, so the
    builder cannot express "deal at random again". The engine falls back to a real shuffle only when
    `HoldemEngineOptions.Instance.CardShuffler` is `null`, which the demo can assign directly because
    it has `InternalsVisibleTo`. Read with fact 14: a shuffler is global state, so it has to be
    uninstalled, not merely emptied.
17. **Only three validation failures exist**, all from `Resources/PackResource.resx`: the game id
    already exists, the current hand is completed, the previous hand is not completed.
18. **`Directory.Build.props` sets `TreatWarningsAsErrors` and `AnalysisMode=Recommended`
    repository-wide**, with an empty `WarningsNotAsErrors`. Generated Razor code will fail the build
    without relaxations, and per CLAUDE.md those relaxations belong in `.editorconfig` with a
    justifying comment, never in a `NoWarn` in the csproj.
19. **`.github/` holds `ci.yml` and `deploy-demo.yml`.** `deploy-demo.yml` declares a `permissions:`
    block, which means every scope it does not list is dropped to *none*.
20. **AntDesign has no catch-all for unknown parameters.** The base chain `AntDomComponentBase` to
    `AntComponentBase` exposes only `Class`, `ClassMapper`, `ComponentIdGenerator`, `ConfigProvider`,
    `Id`, `Ref`, `RTL`, `Style`, `IsDisposed`, `Js`, `RefBack`, and there is no
    `CaptureUnmatchedValues`. An attribute that is not a real `[Parameter]` is an
    `InvalidOperationException` thrown when parameters are set at render time — legal C#, legal Razor,
    all gates green, application dead. Reflect over `AntDesign.dll` before adding an attribute;
    guessing is what put two dead parameters into shipped markup.
    <br>Related traps in the same package: `ButtonType`, `ButtonShape` and `AlertType` are static
    classes of `string` constants rather than enums, so `Type="primary"` and
    `Type="@ButtonType.Primary"` both bind. `Modal`'s `Footer`, `OkText` and `CancelText` are
    `OneOf<string, RenderFragment>?` (AntDesign 0.17.0 depends on `OneOf` 2.1.155), which a plain
    string binds to through a compiler-inserted conversion, while `Drawer`'s `Width` and `Height` are
    plain `int`. And a `Select`'s options must go in its `SelectOptions` fragment, not in
    `ChildContent`, which `Select` never renders — the symptom is a dropdown showing `No Data`.
21. **The gates cannot see the browser.** `dotnet build`,
    `dotnet format OpenHoldemEngine.sln --verify-no-changes` and `dotnet test` are the only checks the
    task runner has, and each sees less than the last layer of defect needs:
    <br>— bUnit renders markup but computes **no geometry and no styles**. Element size, stacking
    order, hit-testing, scroll position and resolved custom properties are all invisible to
    `dotnet test`. A fixed full-screen overlay that swallowed every click, a `scrollIntoView` that
    dragged the page off its own left edge, and a grid that squeezed its rows until the side panels
    painted over the table all shipped with every gate green and an empty console.
    <br>— CSS is not analysed at all, so a stylesheet can parse, ship and do nothing.
    <br>The honest gate for a layout defect is a real browser: `src/OpenHoldemEngine.Demo.BrowserTests`
    (Playwright + MSTest, behind `OHE_TEST_BROWSER_GATE`, wired into `ci.yml`) covers horizontal
    overflow, hit-testing of the action controls, the scroll container and console errors. It covers
    nothing else, so a Definition of done outside its reach must name a measurement — a console probe
    with the before-value quoted — never an impression.
22. **A `:root` in a scoped stylesheet never matches.** Blazor's scoped-CSS rewriter appends the
    component's `[b-xxxxxxxxxx]` attribute to *every* selector in a `.razor.css`, `:root` included, and
    the attribute is stamped only on elements that component renders — `<html>` is not one of them. A
    `var()` with no fallback and no definition then makes the whole declaration invalid at
    computed-value time, so `border: 1px solid var(--undefined)` computes to *no border at all* rather
    than to a default one. Define custom properties in the global `wwwroot/css/app.css`, never in a
    `.razor.css`. The same rewriter is why a class written on a wrapper element in a *parent*
    component is never matched by the child's own stylesheet, and why `::deep` exists.

## A known, deliberate duplication

The PHH tooling (`PhhReader`, `PhhActionParser`, `PhhCards`, `PhhHandDeck`, `PhhAction`, `PhhHand`) is
`internal` inside the *test* project, and the agreed constraint is `InternalsVisibleTo` only, so the
demo carries its own port under `OpenHoldemEngine.Demo.Phh`. This is duplicated source, accepted
knowingly: the rejected alternative, a `<Compile Include>` pointing into the test project, drags a
foreign namespace into the demo assembly (breaking `IDE0130`, an error here) and couples the shipped
demo to a test project. If it becomes a maintenance problem, extract a shared `OpenHoldemEngine.Phh`
project that both consumers reference.

## Issue conventions

- Labels: `demo` on every issue, plus one of `blazor`, `phh`, `ci` or `docs`. Milestone: `Blazor demo`.
- **The issue is the task.** This document holds no task text, so an issue body must stand entirely on
  its own: why the defect exists, what to change, which files, how it is verified, and the subset of
  [Engine facts every task depends on](#engine-facts-every-task-depends-on) that the task can trip
  over. Do not write an issue that only makes sense after reading something else.
- **Issues are created in the order they should be implemented.** `run-next-demo-task.ps1` takes the
  open issues of the milestone in ascending issue number and picks the first one that is ready, so
  creation order *is* execution order. Create a whole batch in one pass, in the order it should run.
- **No dependency lines.** The runner still parses a `Depends on:` line if it is there, but a wrong
  number in it does not fail loudly — it makes the runner skip the task forever. Order the work
  instead of declaring a graph.
- Where a task cannot be settled by `dotnet build` / `dotnet format --verify-no-changes` /
  `dotnet test` — see fact 21 — the issue must carry the exact browser probe that settles it, with the
  measured before-values quoted. "It looks right in the browser" is not a Definition of done.
- **Keep this document small.** A new batch of issues adds a fact here only if it is a repository-wide
  trap the next task would otherwise re-learn, and a verification check only if no gate can run it.
  Per-defect measurements, before-values and history stay in the issue.

## End-to-end verification

These are the checks no automated gate performs. Run them by hand once a batch closes, and treat a
failure here as a new issue in the `Blazor demo` milestone rather than as a note in a pull request.
Run the layout ones at **1500px and at 1280px wide**: the layout switches to a single column below
1366px, so a defect on either side of that breakpoint hides from a single width.

**Any check that starts the dev server must end by stopping it.** Press `Ctrl+C` in its terminal, then
confirm nothing is left listening:

```powershell
Get-NetTCPConnection -LocalPort 5170,7226 -State Listen -ErrorAction SilentlyContinue |
    Select-Object LocalPort, OwningProcess
```

An empty result means the ports are free. A run is not finished while a listener is still up.

1. `dotnet build` and `dotnet test` from the repository root — the analyzer relaxations for the demo
   must not have loosened anything for the library or the tests.
2. `OHE_TEST_BROWSER_GATE=1 dotnet test src/OpenHoldemEngine.Demo.BrowserTests` passes. Then revert one
   layout fix in a working tree and confirm the gate goes red — a gate that has never been seen to fail
   has not been shown to work — and put it back.
3. Open the demo with the console open and touch nothing: no exception, no `does not have a property
   matching the name`, no yellow bar in the footer.
4. Create a six-handed game and play a hand to showdown, then start the next one. The table, the
   database inspector and the call log stay consistent at every step, and the console stays clean.
5. Take every action through the action drawer: the trigger appears for the seat on the clock and is
   its own hit target, the drawer applies the action and closes, and with it closed nothing invisible
   is left over the page.
6. In the inspector after that hand: `HandAction` holds rows keyed on `(GameId, HandNo, No, SeatNo)`,
   with `Street` and `ActionType` stored as text and cards as two-character strings.
7. `getComputedStyle(document.documentElement).getPropertyValue(name)` is non-empty for every
   `--ant-*` and `--color-*` name the demo's stylesheets use. See fact 22 for why this is a computed
   value and not a reading of the stylesheet.
8. The inspector's dropdown lists the engine's tables, shows no `No Data`, and picking a different
   table swaps the columns and rows. The grid's cells have visible borders and the header row a grey
   background.
9. The inspector's grid and the engine-call list each scroll inside their own bounded box: the panels
   stop growing as rows and calls accumulate, and neither pushes the page sideways. The call log
   follows the newest entry after each action, and returns to it after the list is scrolled by hand.
10. Both companion panels are collapsed when the game page opens, and expanding one does not disturb
    the table beside it.
11. Reading the seats clockwise from the top-left of the table gives ascending seat numbers.
12. The companion column beside the table is no wider than 30% of the table's own width, and no table,
    code block or button inside it pushes the page into a horizontal scrollbar.
13. Import the bundled sample, replay a hand step by step, and confirm the final stacks match the
    recorded `finishing_stacks`. Then export that hand to `.phh`, re-import it, and confirm the action
    sequence is identical.
14. Import a hand history, replay it to the end, then start a **normal** game from the same page: it
    must deal random cards and not fail (fact 16).
15. Read the page for leaked source text — an alert with no colour or icon, a visible `.ToString("F2")`,
    a stray `m` after the big blind, a literal `(#{...})` beside a replayed hand's name.
16. Dismiss the walkthrough, reload, and confirm it stays closed. Reopen it from the header button and
    confirm it starts at step 1; close it with the X and confirm the X dismisses rather than advances.
17. Force a failure in one panel — comment out a guard, or point a component at a null — and confirm
    the rest of the page still works and the failing panel shows a message with a Retry that recovers.
    Put the code back afterwards.
18. `dotnet publish -c Release`, then serve `wwwroot` from a sub-path locally, to confirm the
    `base href` rewrite and `404.html` work. Then confirm the deployed site at its Pages URL loads,
    including a deep link, once `deploy-demo.yml` has run green on `main`.
