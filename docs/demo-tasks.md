# Blazor WebAssembly demo — implementation task breakdown

## Purpose of this document

`OpenHoldemEngine` ships as a class library with no executable. Today the only way to see it work is
to read its tests. This breakdown plans a public demo, published to GitHub Pages, that doubles as a
teaching aid: a visitor plays a hand, watches the rows the engine writes to its database, and reads
the exact `ApplyHoldemActionAsync` calls that produced them.

Each numbered item below is meant to become one GitHub issue and one pull request. Every item is
self-contained: an AI assistant should be able to implement it from the issue text plus the
repository, without reading the rest of this document. The shared background an assistant needs is
collected under [Engine facts every task depends on](#engine-facts-every-task-depends-on); the
relevant subset is repeated inside each task's **Notes** section.

Phases A to E shipped as issues #3 to #19. [Phase F](#phase-f--post-release-fixes) was added
afterwards, once the demo was actually run in a browser: it collects the defects the chain could not
catch. That is not an accident of process but a property of it — the automated gates are
`dotnet build`, `dotnet format --verify-no-changes` and `dotnet test`, and every Phase F defect passes
all three. A duplicated SQL quote, a `bool` where a `string` comes back from JS interop, and a missing
`@` on a Razor attribute are all perfectly legal code.

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
Every one of them was verified against the code at `b7c09ea`.

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

Facts 1 to 18 describe the repository as task 1 found it. Phase F adds three more, verified against
`fa656c3` — the state after the whole chain had landed.

19. **`.github/` exists now**, holding `ci.yml` and `deploy-demo.yml` from task 15, so fact 18 is
    history rather than current state. `deploy-demo.yml` declares a `permissions:` block, which means
    every scope it does not list is dropped to *none* — the reason task 18 exists.
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

## A known, deliberate duplication

The PHH tooling (`PhhReader`, `PhhActionParser`, `PhhCards`, `PhhHandDeck`, `PhhAction`, `PhhHand`)
is `internal` inside the *test* project. Since the agreed constraint is `InternalsVisibleTo` only —
no restructuring — the demo gets its own port of these types under `OpenHoldemEngine.Demo.Phh`
(task 11) rather than a shared project.

This is duplicated source, accepted knowingly. The rejected alternative was
`<Compile Include="..\OpenHoldemEngine.Tests\Phh\*.cs" Link="..." />`, which drags a foreign
namespace into the demo assembly (breaking `IDE0130`, an error here) and couples the shipped demo to
a test project. If the duplication becomes a maintenance problem, the follow-up is to extract a
shared `OpenHoldemEngine.Phh` project that both consumers reference; task 11 leaves a note pointing
at that option.

---

# Task chain

Seventeen tasks in five phases, plus eight post-release fixes in Phase F. Each is sized for a single
pull request.

## Phase A — foundation

### 1. Scaffold the Blazor WebAssembly demo project

**Depends on:** nothing.

**Why.** Everything else needs a project that builds under the repository's analyzer settings. That
is not a formality here: `Directory.Build.props` turns every style and quality warning into an error
repository-wide, and generated Razor code trips several of them. Getting a green build now means no
later task has to fight the toolchain.

**Scope.**
- Create `src/OpenHoldemEngine.Demo` from the `blazorwasm` template, standalone, `net8.0`, and add it
  to `OpenHoldemEngine.sln`.
- Add a `ProjectReference` to `src/OpenHoldemEngine/OpenHoldemEngine.csproj`.
- Add the `AntDesign` package (latest version compatible with net8.0) and wire it up:
  `builder.Services.AddAntDesign()`, the stylesheet in `index.html`, and an `AntContainer` component
  in the root layout so modals and notifications have a host.
- Relax the analyzer rules that generated Razor code cannot satisfy. Add `.editorconfig` sections for
  `[src/OpenHoldemEngine.Demo/**.cs]` and `[**/*.razor]` covering at minimum `CS1591` and `IDE0130`
  for generated partial classes. Each suppression carries a comment saying why, per CLAUDE.md. Do not
  add a blanket `NoWarn` to the csproj.
- Replace the template's sample pages with a single placeholder home page.

**Files.** `src/OpenHoldemEngine.Demo/**` (new), `OpenHoldemEngine.sln`, `.editorconfig`.

**Definition of done.**
- `dotnet build` from the repository root succeeds with zero warnings.
- `dotnet format OpenHoldemEngine.sln --verify-no-changes` passes.
- `dotnet run --project src/OpenHoldemEngine.Demo` serves a page rendering at least one Ant Design
  component, proving the CSS and the service registration are both in place.

**Notes for the implementer.** The `.editorconfig` is 16 KB and `root = true`; read the existing
per-path sections (there is already a `[**/*Tests.cs]` one) and follow their comment style. Keep
`charset = utf-8-bom` for `.cs` files — it is enforced.

---

### 2. Host the engine in the browser on SQLite (walking skeleton)

**Depends on:** 1.

**Why.** This is the highest-risk task in the project. EF Core with `Microsoft.Data.Sqlite` under
WebAssembly needs a native SQLite build, which means the `wasm-tools` workload and
`WasmBuildNative`. If that cannot be made to work, the database-inspector design (task 10) has to
change. Proving it end to end before any UI exists keeps that discovery cheap.

**Scope.**
- Add an `InternalsVisibleTo` entry for `OpenHoldemEngine.Demo` to
  `src/OpenHoldemEngine/OpenHoldemEngine.csproj`, next to the existing test-project entry. This is
  the only change to the library in the entire project.
- Add `Microsoft.EntityFrameworkCore.Sqlite` 9.0.1 to the demo — the version the engine and the test
  project already use.
- Write `Engine/DemoEngineHost.cs`, porting the pattern in
  `src/OpenHoldemEngine.Tests/TestEngineHost.cs`: construct one
  `SqliteConnection("DataSource=:memory:")`, hold it open for the lifetime of the app, call
  `services.AddHoldemEngine(opts => opts.UseSqlite(connection), configure)`, then resolve
  `IDbContextFactory<HoldemDbContext>` and call `EnsureCreated()`.
- Register that same `SqliteConnection` in DI as a singleton in its own right. Task 10 queries it
  directly, and it must be the *same* instance or the in-memory database will look empty.
- Enable the WebAssembly native build: install the `wasm-tools` workload, set `WasmBuildNative`, and
  document both in the demo README.
- A temporary diagnostics page: create a game with `HoldemActionCreateGame`, then list the table
  names from `sqlite_master`.

**Files.** `src/OpenHoldemEngine/OpenHoldemEngine.csproj`,
`src/OpenHoldemEngine.Demo/Engine/DemoEngineHost.cs`, `src/OpenHoldemEngine.Demo/Program.cs`,
`src/OpenHoldemEngine.Demo/README.md`.

**Definition of done.**
- The diagnostics page, in a real browser, creates a game and prints the nine engine tables.
- The demo README records the measured `dotnet publish -c Release` output size and the cold-start
  time, so later tasks can tell whether they made things worse.

**Notes for the implementer.**
- The kept-alive connection is what stops SQLite from dropping the schema between the `DbContext`
  instances the factory hands out. Do not let it be disposed.
- `HoldemDbContext` is `internal`; the `InternalsVisibleTo` above is what makes
  `GetRequiredService<IDbContextFactory<HoldemDbContext>>()` compile.
- The engine pulls in `HandEvaluator` 1.0.1, an old library with no trimming annotations that builds
  large static lookup tables at type initialisation. Watch the published size and the first-hand
  latency, and report both even if they are acceptable.
- If `Microsoft.Data.Sqlite` cannot be made to work under WebAssembly, do **not** close this task
  quietly with a workaround. Record the failure, open a follow-up issue for the fallback
  (`Microsoft.EntityFrameworkCore.InMemory` plus a synthetic table listing), and flag that it
  invalidates task 10's contract.

---

### 3. Demo session state and the engine call journal

**Depends on:** 2.

**Why.** Three separate UI panels have to agree on one truth: the last `GameData` the engine
returned, and the ordered list of calls that produced it. Centralising that now stops the table, the
inspector and the call log from each keeping their own copy.

**Scope.**
- `Services/DemoGameSession.cs` — the *only* place in the demo that calls
  `HoldemEngine.ApplyHoldemActionAsync`. It owns the current `GameId`, caches the last successful
  `GameData`, exposes the current `HandData` as explicitly nullable, and raises a `StateChanged`
  event that components subscribe to.
- Failure handling: a failed `FluentResults.Result` becomes a user-facing message and leaves the
  cached state untouched — the engine short-circuits before `SaveChangesAsync`, so nothing was
  persisted and nothing needs rolling back.
- `Services/EngineCallJournal.cs` — an ordered log of entries holding the `HoldemAction` that was
  sent, whether it succeeded, the error messages if not, and a sequence number. This journal feeds
  task 9 (the C# call viewer) and task 14 (PHH export), so design it to be inspected and replayed,
  not merely displayed.
- Convenience methods on the session for the flows the UI needs: create game, start first hand, start
  next hand, apply a player action.

**Files.** `src/OpenHoldemEngine.Demo/Services/DemoGameSession.cs`,
`src/OpenHoldemEngine.Demo/Services/EngineCallJournal.cs`, DI registration in `Program.cs`.

**Definition of done.** Task 16's tests drive create → start hand → fold through the session and
assert both the resulting `GameData` and that the journal holds exactly those three calls in order.

**Notes for the implementer.**
- `GameData.HandData` is `default!` — genuinely null — between `CreateGame` and the first
  `StartHand`. The null-check belongs here, once, not in every component.
- Use `HoldemActionCreateGame` and `HoldemActionStartHand`. `HoldemActionStartGame` and
  `HoldemActionFinishGame` are public but unwired and throw `NotImplementedException`.
- The three possible engine errors are the game id already existing, the current hand being
  completed, and the previous hand not being completed. Map them to something friendlier than the
  raw resource strings.

---

### 4. Application shell and layout

**Depends on:** 1.

**Why.** The demo's whole argument is that three things move together: the table, the database, and
the engine calls. The layout has to make that simultaneity visible on one screen rather than hiding
two thirds of it behind navigation.

**Scope.**
- An Ant Design shell with the poker table as the primary region and the database inspector and
  engine call log as collapsible companion panels, all visible at once on a laptop-sized viewport and
  degrading to stacked or tabbed panels when narrow.
- A header with the project name and a link to the GitHub repository.
- A dismissible `Alert`, shown on first visit and remembered in LocalStorage, explaining that the
  database lives in the tab's memory, that reloading the page discards the game, and that `.phh`
  export is how to keep a hand. This is the notification the storage decision requires — plainly
  worded, not a footnote.

**Files.** `src/OpenHoldemEngine.Demo/Layout/**`, `src/OpenHoldemEngine.Demo/Pages/Home.razor`,
`src/OpenHoldemEngine.Demo/wwwroot/css/**`.

**Definition of done.** The layout builds with placeholder content in each of the three regions, the
notification appears on a first visit and stays dismissed after a reload, and the page is usable at
1366x768 without horizontal scrolling.

**Notes for the implementer.** The panels will later re-render on every engine call. Keep component
boundaries tight enough that one panel updating does not force a full-page re-render.

---

## Phase B — the poker table

### 5. Game setup and start controls

**Depends on:** 3, 4.

**Why.** The visitor's first interaction. It also has to be honest about which `GameOptionData`
fields the engine actually reads, because three of them are decorative.

**Scope.**
- A setup form: `MaxPlayersCount` (2 to 6), `InitialSmallBlind` (with the big blind shown as twice it,
  read-only, and a note that the engine derives it), `InitialDealerSeatNo`, and a per-player list of
  nickname, `SeatNo` and `BuyInAmount` with sensible defaults so the visitor can start in one click.
- A "Create and start" action that sends `HoldemActionCreateGame`, then `HoldemActionStartHand` with
  the assembled `JoinSeats`.
- Options the engine ignores — `AllowRebuy` on `GameOptionData`, and `BuyIns` / `KickOffSeats` on
  `HoldemActionStartHand` — are either omitted or rendered disabled with the label "declared by the
  DTO but not implemented by the engine". Do not let the UI imply they work.

**Files.** `src/OpenHoldemEngine.Demo/Components/Table/GameSetup.razor` and its code-behind.

**Definition of done.** A game starts from the form and the table shows a dealt hand with blinds
posted.

**Notes for the implementer.**
- `JoinSeats` must be in ascending `SeatNo` order — `StartHandActionHandler` walks the array in order
  to place the very first button.
- Do not use `HoldemActionStartGame`; it is public but unwired and throws
  `NotImplementedException`.
- Heads-up is not just a smaller ring: with two funded seats the button posts the small blind, acts
  first pre-flop and last on every later street. The engine handles this; the UI must not assume
  "small blind is always the seat after the button".
- Seat numbers are 1-based.

---

### 6. Poker table component

**Depends on:** 5.

**Why.** This is the demo's centrepiece and the thing a screenshot has to sell.

**Scope.**
- `Components/Table/` renders each `SeatData`: `SeatNo`, nickname, `Stack` (with the delta against
  `StartHandStack`), hole cards, the last action, and dealer / small blind / big blind badges derived
  from `HandData.DealerSeatNo`, `SmallBlindSeatNo` and `BigBlindSeatNo`.
- Visual states: the seat on the clock (`NextPlayerSeatNo`), folded (`SeatData.IsFolded`), and all-in
  (`SeatData.IsAllIn`).
- The centre of the table shows `CommunityCards`, the pot, the hand number and the current street.
- A reusable card component with suit colours and glyphs.

**Files.** `src/OpenHoldemEngine.Demo/Components/Table/PokerTable.razor`, `Seat.razor`,
`PlayingCard.razor` and their styles.

**Definition of done.** A hand played by hand through the UI renders correctly at every step,
including a fold-to-one-player finish and an all-in.

**Notes for the implementer.**
- `HandData.Pot` is prior streets only and `StreetPot` is the current street. The total pot is their
  sum — display both if it helps explain the model, but never show `Pot` alone labelled "pot".
- `SeatData.BetSize` is always `null`. The mapper never fills it. Derive the current wager from the
  action history in the session if you need it, or leave it out.
- Suit glyphs live in a `[Description]` attribute on `CardSuit`; there is no public helper, so read
  the attribute yourself.
- `Card.ToString()` produces `"ah"`, `"tc"` — rank first, lowercase.
- Seats arrive occupied-only and ascending by `SeatNo`; there is no empty-seat padding.

---

### 7. Action controls driven by `Evaluations`

**Depends on:** 6.

**Why.** `HandData.Evaluations` is the engine's answer to "what may this seat do right now", and
building the controls from it — rather than from poker knowledge — is exactly the lesson the demo
exists to teach. It is also the single easiest place to introduce a subtle bug.

**Scope.**
- Render an action button only when the matching `PlayerActionType` appears in
  `HandData.Evaluations`: Fold, Check, Call (labelled with the amount), Raise.
- Raise uses a slider and a numeric input bounded by the evaluation's `MinValue` and `MaxValue`, with
  shortcut buttons for half pot, pot and all-in, each clamped to the legal window.
- Controls are disabled entirely when `NextPlayerSeatNo == 0`.
- Every action goes through `DemoGameSession`, so the table, the database inspector and the call log
  all refresh together.

**Files.** `src/OpenHoldemEngine.Demo/Components/Table/ActionPanel.razor` and its code-behind.

**Definition of done.**
- It is not possible to submit an action that is absent from `Evaluations`, nor a size outside the
  advertised window.
- The amount shown on the Call button matches the chips the engine actually moves.
- A short-stacked seat facing a raise renders an All-in button rather than an unusable raise slider.

**Notes for the implementer — read this twice.**
- The two evaluation kinds use **different bases on purpose**. A `Call`'s `MinValue`/`MaxValue` is an
  *increment*: how much more this seat must put in. A `Raise`'s `MinValue`/`MaxValue` are both an
  *absolute street total*: raise the street wager **to** this. `HoldemActionPlayerAction.Size` for a
  raise expects that absolute value, unmodified.
- Fold and Check are sent with `Size = 0`; the engine computes the call amount itself.
- When the raise window is degenerate the engine emits a synthetic `Raise(allInTo, allInTo)` where
  `allInTo` is the seat's stack plus its current street wager. Render that as All-in, not as a
  one-point slider.
- Half-pot and pot shortcuts must be computed against `Pot + StreetPot`, then converted to an
  absolute "raise to" value before clamping.

---

### 8. Street progression, showdown and the next hand

**Depends on:** 7.

**Why.** Without this the demo plays one street and stops. It is also where the engine's results —
`Net`, `RankType`, side pots — become visible.

**Scope.**
- Show the street changing and the board growing, with a visible marker for each new street.
- At showdown, reveal hole cards and show each seat's `HandResultData`: `Net` and `RankType`, plus
  the resulting stack change.
- Detect hand completion (`NextPlayerSeatNo == 0` with empty `Evaluations`) and offer "Next hand",
  which sends `HoldemAction.CreateStartNextHandDto`. Also offer "New game", which resets the session.
- A compact hand history strip for the current game: hand number, winner, pot size.

**Files.** `src/OpenHoldemEngine.Demo/Components/Table/HandResult.razor`,
`StreetIndicator.razor`, plus session methods from task 3.

**Definition of done.** Three or more consecutive hands play through, the button rotates, blinds move
with it, and stacks carry over between hands.

**Notes for the implementer.**
- `Street` is not sequential: `PreFlop = 0, Flop = 3, Turn = 4, River = 5, Showdown = 6,
  Finished = 7`. The three middle values encode the community-card count. `StreetExtensions.Next()`
  is `internal`, so define the demo's own ordering rather than relying on enum arithmetic.
- `HandResultData.Description` is always `null`; use `RankType` for the category name.
- Pot splits are rounded to the cent, with the remainder going to the last winner — a split pot will
  not always divide evenly, and that is correct behaviour, not a bug to hide.
- Starting a hand while one is still running fails with "The previous hand is not completed"; the
  Next-hand button must only appear once the hand is genuinely over.

---

## Phase C — the showcase panels

### 9. Engine call viewer with C# syntax highlighting

**Depends on:** 3, 4.

**Why.** This is what turns a poker game into documentation. The visitor sees the exact call that
produced the state change they just watched, and can paste it into their own project.

**Scope.**
- For each `EngineCallJournal` entry, generate a readable C# snippet: the DTO constructed with named
  arguments, and the awaited call, followed by a collapsed one-line summary of the result.

  ```csharp
  var result = await engine.ApplyHoldemActionAsync(
      new HoldemActionPlayerAction(
          GameId: Guid.Parse("..."),
          ActionType: PlayerActionType.Raise,
          Size: 300m));
  ```

- Read-only rendering with C# syntax highlighting via **Prism.js vendored into `wwwroot`** — not a
  CDN, so the page stays self-contained on GitHub Pages and needs no external origin.
- Auto-scroll to the newest entry, with "copy snippet" per entry and "copy whole session" for the
  lot. The whole-session copy emits a snippet that compiles as-is in a console project, including the
  `using` directives and the host setup.
- Failed calls are shown with their error message rather than hidden.

**Files.** `src/OpenHoldemEngine.Demo/Components/EngineCalls/**`,
`src/OpenHoldemEngine.Demo/Services/EngineCallSnippetWriter.cs`,
`src/OpenHoldemEngine.Demo/wwwroot/lib/prism/**`.

**Definition of done.** Every game action adds exactly one highlighted entry, and the whole-session
copy pasted into a fresh console project compiles.

**Notes for the implementer.**
- Snippet generation belongs in a plain C# service, not in the Razor markup — task 16 tests it
  directly.
- Format decimals with an `m` suffix and the invariant culture; a comma decimal separator would not
  compile.
- Highlighting is a one-shot call after render; guard against re-highlighting already-processed nodes
  when the list grows.

---

### 10. Live database inspector

**Depends on:** 2, 4.

**Why.** The engine's central claim is "there is no in-memory game object, the database is the
state". This panel is the proof. It must show the *physical* schema, which is why it deliberately
bypasses EF.

**Scope.**
- Query the same `SqliteConnection` the engine writes through: table list from `sqlite_master`
  (excluding `sqlite_%`), columns from `PRAGMA table_info`, rows with `LIMIT`/`OFFSET`, and a row
  count per table.
- A table picker plus an Ant Design grid with paging, refreshed after every engine call.
- Show the selected table's `CREATE TABLE` statement, so composite keys and value conversions are
  visible.
- Highlight tables whose row count changed since the previous action, so the visitor can see which
  writes a given action caused.

**Files.** `src/OpenHoldemEngine.Demo/Services/SqliteBrowser.cs`,
`src/OpenHoldemEngine.Demo/Components/Database/**`.

**Definition of done.**
- After one hand, the inspector shows rows in `HandAction`, `HandDeckCard`, `HandHoleCard` and
  `HandPlayerEndState`.
- Tables with composite primary keys, such as `HandAction` keyed on
  `(GameId, HandNo, No, SeatNo)`, render correctly.

**Notes for the implementer.**
- Do not use `HoldemDbContext` here at all, even though `InternalsVisibleTo` makes it reachable. The
  point of the panel is the physical schema.
- Value converters mean `Street`, `ActionType` and `RankType` are stored as text, and cards as
  two-character strings like `"ah"`. Show them as they are stored; that *is* the lesson.
- `HandAction.Amount` is a seat's cumulative wager for the street, `AddAmount` the increment and
  `Size` the requested size. A short column-header tooltip explaining this makes the panel far more
  useful.
- Quote identifiers in generated SQL, and never interpolate a table name that did not come from
  `sqlite_master`.

---

## Phase D — hand histories

### 11. PHH domain model and reader in the demo project

**Depends on:** 1.

**Why.** Import, replay and export all need a PHH object model. The existing one is `internal` inside
the test project and cannot be referenced, so the demo needs its own — see
[A known, deliberate duplication](#a-known-deliberate-duplication).

**Scope.**
- Port `PhhAction`, `PhhHand`, `PhhActionParser`, `PhhCards`, `PhhReader` and `PhhHandDeck` from
  `src/OpenHoldemEngine.Tests/Phh/` into `src/OpenHoldemEngine.Demo/Phh/`, as public types in
  namespace `OpenHoldemEngine.Demo.Phh`.
- Add the `Tomlyn` 0.19.0 package — the same version the test project uses.
- Extract the support check that currently lives inside `PhhReplayRunner.IsSupported` into a
  standalone `PhhSupport.IsSupported(hand, out string reason)`, keeping the existing human-readable
  rejection reasons verbatim.
- Add an XML doc comment on each public type explaining why it exists, and a file-level note pointing
  at the test-project original and at the possible future consolidation into a shared
  `OpenHoldemEngine.Phh` project.
- Do **not** port `PhhFixture` or `PhhReplayRunner`; the first is about embedded test corpora and the
  second is assertion-heavy test scaffolding.

**Files.** `src/OpenHoldemEngine.Demo/Phh/**`, `src/OpenHoldemEngine.Demo/OpenHoldemEngine.Demo.csproj`.

**Definition of done.** Task 16's tests parse a sample `.phhs` and reproduce the parse of several
reference hands, including one with unknown `??` cards.

**Notes for the implementer.**
- Keep the regex that recovers the source-filename comment above each `[N]` table: Tomlyn's object
  model discards comments, so that information is only available from the raw text.
- `PhhHandDeck.Create` deterministically fills unknown cards from the remaining deck, which is what
  makes a failing replay fail identically every run. Preserve that determinism.
- `PhhCards.Parse` lowercases before calling `Card.FromString`; PHH writes `"Ah"` and the engine
  writes `"ah"`.
- These files move out of the `[**/*Tests.cs]` `.editorconfig` exemption, so they must satisfy the
  full naming rules, including `CA1707`.

---

### 12. Import a hand history file

**Depends on:** 11, 4.

**Why.** The import half of the hand-history requirement, and the thing that makes the demo
interesting to someone who arrives with their own hands.

**Scope.**
- File selection through `InputFile` plus drag-and-drop, accepting `.phh` and `.phhs`.
- Parse, then list the contained hands with their metadata: source, players, blinds, action count,
  and whether the engine can replay them, with the rejection reason shown for those it cannot.
- Selecting a hand loads it as the replay subject for task 13.
- Ship one small bundled sample file so the feature is explorable without the visitor supplying
  anything. Prefer a couple of hands from the same open PHH dataset the tests already vendor, with
  the same MIT attribution as
  `src/OpenHoldemEngine.Tests/Resources/phh/README.md`.

**Files.** `src/OpenHoldemEngine.Demo/Components/Phh/HandHistoryImport.razor`,
`src/OpenHoldemEngine.Demo/wwwroot/sample/**`, plus attribution in the demo README.

**Definition of done.** A ten-hand file imports, every hand is listed, and unsupported hands carry a
human-readable reason rather than silently disappearing.

**Notes for the implementer.**
- Seat mapping comes straight from the PHH spec: player `pI` becomes `SeatNo = I`, and
  `InitialDealerSeatNo = PlayerCount`, so the last player is on the button.
- Heads-up is the exception where the spec's two rules collide: with two players the *last* player
  posts the small blind, not `p1`. `PhhReplayRunner.BlindSeatsFor` is the reference.
- Parsing a large `.phhs` on the UI thread will freeze the tab; parse off the render path and show
  progress.
- Attribution for any bundled dataset content is mandatory — carry the licence note.

---

### 13. Replay an imported hand step by step

**Depends on:** 12, 8, 9.

**Why.** Replay is what ties the whole demo together: a real recorded hand drives the engine while
the table, the database inspector and the call log all move in lockstep. It is also the strongest
evidence that the engine is correct, because the recorded finishing stacks are an oracle.

**Scope.**
- An `ICardShuffler` implementation backed by `PhhHandDeck`, mirroring
  `src/OpenHoldemEngine.Tests/TestCardShuffler.cs`, supplied through
  `HoldemEngineOptionsBuilder.WithCardShuffler` so the engine deals the recorded cards.
- Replay controls: Next, Auto-play with an adjustable speed, and Reset.
- Each step goes through the same `DemoGameSession` as manual play, so all three panels stay in sync
  and the call log shows the replay's calls in the same form as a human's.
- Map PHH decisions to engine actions: `f` becomes `Fold`; `cc` becomes `Check` when `Check` appears
  in `HandData.Evaluations` and `Call` otherwise; `cbr X` becomes `Raise` with `Size = X` passed
  through unchanged.
- Before each step, check the recorded actor against `NextPlayerSeatNo`. A divergence, or an engine
  rejection, is surfaced as an in-UI error naming the offending PHH action — never an unhandled
  exception.
- When the hand carries `finishing_stacks`, compare them against the engine's result and show the
  verdict.

**Files.** `src/OpenHoldemEngine.Demo/Phh/PhhCardShuffler.cs`,
`src/OpenHoldemEngine.Demo/Services/PhhReplayController.cs`,
`src/OpenHoldemEngine.Demo/Components/Phh/ReplayControls.razor`.

**Definition of done.** A hand from the Pluribus corpus replays to completion in the browser, and the
UI shows that the final stacks match the recorded `finishing_stacks`.

**Notes for the implementer.**
- `ICardShuffler.GetDeckCardsAsync` is called again before **every** street change, so it must return
  the same board for the same hand each time. `GetHoleCardsAsync` is called per seat, by ascending
  real `SeatNo`.
- Each replayed hand is its own game: recorded corpora reset stacks every hand, and `BuyIns` can only
  top a stack up — and is not read by any handler anyway.
- `HoldemEngineOptions` is a process-wide singleton and `HoldemEngine` is registered as a singleton
  over it, so the shuffler cannot be swapped per game. Set it for the replay session and clear it
  when returning to manual play; do not attempt to run manual play and replay concurrently.
- `cbr X` and `Raise` share a basis — both mean "bring the street wager up to X" — so no arithmetic
  is needed, and adding any is a bug.

---

### 14. Export the current hand as PHH

**Depends on:** 8, 11.

**Why.** The export half of the requirement, and — given the deliberate lack of persistence — the
only way a visitor keeps anything. Nothing in the repository writes PHH today, so this is new code
rather than a port.

**Scope.**
- A writer producing a spec-conformant hand: `variant = "NT"`, `antes`, `blinds_or_straddles`,
  `min_bet`, `starting_stacks`, `players`, `actions`, and `finishing_stacks`.
- Reconstruct the action list from the `EngineCallJournal` plus the resulting `GameData` — hole cards
  from `SeatData.Hole`, board cards from `HandData.CommunityCards` grouped by street. Emit
  `d dh pI <cards>`, `d db <cards>`, `pI f`, `pI cc` and `pI cbr X`.
- Export the current hand as `.phh`, or the whole session as a `.phhs` with numbered `[N]` tables.
- Download through JS interop.

**Files.** `src/OpenHoldemEngine.Demo/Phh/PhhWriter.cs`,
`src/OpenHoldemEngine.Demo/Components/Phh/HandHistoryExport.razor`,
`src/OpenHoldemEngine.Demo/wwwroot/js/download.js`.

**Definition of done.** Round trip: export a played hand, re-import the produced file, replay it, and
get the same action sequence and the same finishing stacks. Task 16 covers this as an automated test,
not only as a manual check.

**Notes for the implementer.**
- Build the export from the journal, not from the database tables. The journal already holds the
  caller's intent in the right order; reconstructing it from `HandAction` rows means re-deriving
  street boundaries and increments for no benefit.
- Card notation: the engine emits `"ah"` and PHH wants `"Ah"`, so upper-case index 0 and nothing
  else.
- A raise is written as `cbr` with the absolute street total, which is exactly the `Size` the caller
  passed — no conversion.
- `Tomlyn` can serialise via `Toml.FromModel`, but hand-written output may match the reference
  dataset's layout more closely. Pick one, and say in the pull request why.
- Write the invariant culture for every number; a comma decimal separator produces invalid TOML.
- Fold `finishing_stacks` in only when the hand is actually complete.

---

## Phase E — delivery

### 15. CI and GitHub Pages deployment

**Depends on:** 2.

**Why.** The demo has no value until it is reachable at a URL, and the repository currently has no
automation at all — this adds the first `.github/` directory.

**Scope.**
- `.github/workflows/ci.yml`: on push and pull request, restore, `dotnet build`,
  `dotnet format --verify-no-changes`, and `dotnet test`.
- `.github/workflows/deploy-demo.yml`: on push to `main`, `dotnet workload install wasm-tools`,
  publish the demo in Release, rewrite the `base href` to the repository sub-path, add `.nojekyll`,
  copy `index.html` to `404.html` for SPA deep links, then `actions/upload-pages-artifact` and
  `actions/deploy-pages` with `pages: write` and `id-token: write` permissions.
- Record the published artifact size in the workflow summary so regressions are visible.

**Files.** `.github/workflows/ci.yml`, `.github/workflows/deploy-demo.yml`.

**Definition of done.** The demo loads from its public Pages URL, and a deep link to an inner route
does not 404.

**Notes for the implementer.**
- GitHub Pages serves the site from a sub-path, so a `base href` of `/` will break every asset
  request. This is the single most common failure mode for Blazor WebAssembly on Pages.
- Without `.nojekyll`, Jekyll strips the `_framework` directory and the app never boots.
- Cap the PHH replay corpus in CI with `OHE_TEST_PHH_HAND_LIMIT` — an uncapped run replays 10 000
  hands and takes about four minutes.
- `dotnet test --filter` is silently ignored in this repository; the filter has to be passed to the
  test app itself (see CLAUDE.md).

---

### 16. Tests for the demo logic

**Depends on:** 3, 11, 14.

**Why.** The demo contains real logic that can be wrong quietly: the snippet writer, the PHH writer,
the decision mapping, the session's error handling. None of it is covered by the engine's own tests.

**Scope.**
- A new `src/OpenHoldemEngine.Demo.Tests` project on `MSTest.Sdk/3.6.1`, matching the existing test
  project's setup.
- Cover: PHH round trip (export, re-import, replay, compare); the C# snippet writer's output for each
  action type; the PHH decision to `PlayerActionType` mapping including the `cc` ambiguity; and
  `DemoGameSession` on the happy path and on each of the three engine failures.

**Files.** `src/OpenHoldemEngine.Demo.Tests/**`, `OpenHoldemEngine.sln`.

**Definition of done.** `dotnet test` passes locally and in CI, and the new project runs in well under
a minute.

**Notes for the implementer.**
- Do not re-test the engine. `PhhReplayTests` and `MultiHandSessionTests` already cover its rules
  thoroughly; duplicating them here only slows CI down.
- FluentAssertions is pinned at 6.1.0, so newer spellings such as `AllSatisfy` and
  `BeGreaterThanOrEqualTo` do not exist.
- Test method names keep their underscores, `Subject_Scenario_Expected`, and the `.editorconfig`
  exemption for `CA1707` matches `*Tests.cs` only.
- The engine options singleton means tests that configure a shuffler must not run in parallel; the
  existing test assembly is marked `[assembly: DoNotParallelize]` for exactly this reason.

---

### 17. Demo documentation and polish

**Depends on:** every preceding task.

**Why.** A visitor arrives with no context. If the three panels do not explain themselves within a
few seconds, the demo has failed at the only job it has.

**Scope.**
- A demo `README.md`: what the demo shows, how to run it locally including the `wasm-tools` workload,
  and the known limitations — no persistence across reloads, and `AllowRebuy`, `BuyIns` and
  `KickOffSeats` being declared by the DTOs but ignored by the engine.
- A link to the live demo from the repository root `README.md`.
- A first-visit walkthrough, three short steps, explaining what each panel is for.
- A screenshot or short GIF for the README.
- Verify the Ant Design light and dark themes and the narrow-viewport layout.

**Files.** `src/OpenHoldemEngine.Demo/README.md`, root `README.md`,
`src/OpenHoldemEngine.Demo/wwwroot/img/**`, onboarding component.

**Definition of done.** Someone who has never seen the repository can open the URL and explain,
unprompted, what the three panels are showing.

**Notes for the implementer.** Keep every word English, including UI strings — the repository rule is
English-only throughout, without exception.

---

## Phase F — post-release fixes

Eight tasks, written after the demo was first run in a browser. Six are independent; only 24 and 25
wait on anything.

### 18. Grant `contents: read` to the Pages deployment workflow

**Depends on:** nothing.

**Why.** The Pages deployment has never succeeded. It fails on its first step, `Checkout code`, with
`repository not found` and git exit code 128. The repository is private, but the credential is not the
problem: `deploy-demo.yml` declares a `permissions:` block, and declaring one drops every scope it does
not list to *none*. The failing run's own log header reads `GITHUB_TOKEN Permissions: Metadata: read,
Pages: write` — no `contents: read`, so the token cannot see the repository it is checking out.
`ci.yml` passes on the same private repository because it declares no `permissions:` block and keeps
the default read access.

**Scope.**
- Add `contents: read` to the `permissions:` block, alongside `pages: write` and `id-token: write`.
- Add an `environment:` block to the `build` job — `name: github-pages`, `url` from the deployment
  step's `page_url` output. `actions/deploy-pages@v4` expects that attribution and the job has none.
- Make the `Rewrite base href` step fail loudly. It is a literal `sed` match on `<base href="/" />`, so
  any change to spacing or quoting silently no-ops and ships a site whose every asset request 404s.

**Files.** `.github/workflows/deploy-demo.yml`.

**Definition of done.** `contents: read` and the `github-pages` environment are both present, and the
`base href` step exits non-zero when its match does not apply.

**Notes for the implementer.**
- Do **not** introduce a personal access token. A `PRIVATE_REPO_ACCESS` secret exists and passing it as
  `token:` to `actions/checkout` would also make the step pass, but that treats a missing permission
  scope as if it were a missing credential. `contents: read` never expires and keeps working unchanged
  if the repository is later made public.
- Do not delete the `permissions:` block to inherit the defaults — `id-token: write` is required for
  Pages OIDC and is not a default. The block has to stay; it has to be complete.
- Publishing Pages from a private repository additionally requires an account plan that permits it. If
  `deploy-pages` still fails after this, the next thing to look at is repository settings, not the
  workflow. This task's job ends at getting past `Checkout code`.
- The workflow only runs `on: push` to `main`, so no pull request can prove it green.

---

### 19. Nest every project under the `src` solution folder

**Depends on:** nothing.

**Why.** `OpenHoldemEngine.sln` has a `src` solution folder, but only the two demo projects are nested
inside it, so Solution Explorer shows a split that does not exist on disk: all four projects already
live under `src/`.

**Scope.** Add the two missing `GlobalSection(NestedProjects)` entries, mapping `OpenHoldemEngine` and
`OpenHoldemEngine.Tests` to the `src` folder's GUID. Nothing else — no GUID changes, no `.csproj` path
changes, no configuration-platform edits.

**Files.** `OpenHoldemEngine.sln`.

**Definition of done.** All four projects appear under `src` in the solution tree, and the three gates
pass unchanged.

**Notes for the implementer.**
- **Do not move any directory.** This is a virtual Visual Studio grouping recorded in
  `GlobalSection(NestedProjects)` and unrelated to the filesystem. Moving directories would break every
  relative `ProjectReference`, the `InternalsVisibleTo` wiring, `scripts/pack-phh-*.ps1`, and the paths
  hard-coded in `.github/workflows/`.
- The file is UTF-8 with BOM, CRLF, tab-indented. A line-ending flip turns a two-line change into an
  unreviewable whole-file diff.

---

### 20. Fix the SQLite identifier quoting that breaks the database inspector

**Depends on:** nothing.

**Why.** This is the headline crash. `SqliteBrowser.EscapeIdentifier` returns an identifier *with* its
quotes, and all three of its call sites wrap the result in another pair, so `GetRowCount("Game")`
executes `SELECT COUNT(*) FROM """Game"""` — the table whose name is the seven-character string
`"Game"` — and SQLite answers `no such table`. `GetColumnInfo` returns zero rows silently first (a
`PRAGMA` on an unknown table does not error), then `GetRowCount` throws. Nothing catches it:
`DatabaseInspector.OnInitializedAsync` loads the moment a game exists, and again on every
`StateChanged`, so the exception escapes the renderer and takes the whole page's interactivity down to
the stock error bar. The panel carrying the demo's central lesson — the database *is* the state — is
the one panel that has never worked.

**Scope.**
- Drop the duplicated quotes at the three call sites (`GetColumnInfo`, `GetRowCount`, `GetTableRows`)
  and let `EscapeIdentifier` supply them. The helper is correct; the call sites are wrong.
- `SqliteBrowser_Tests` over a real schema: assert `GetTableNames()` is non-empty, then, for **every**
  name it returns, that all four readers complete and return sensible values. Iterating the real table
  list is what makes the regression impossible to reintroduce quietly.

**Files.** `src/OpenHoldemEngine.Demo/Services/SqliteBrowser.cs`,
`src/OpenHoldemEngine.Demo.Tests/SqliteBrowser_Tests.cs`.

**Definition of done.** `dotnet test` passes with the new tests and the three gates are clean.

**Notes for the implementer.**
- Do not switch to a bound parameter. SQLite does not accept one where an identifier is expected, which
  is exactly why the helper exists.
- Hold the `:memory:` connection open for the test's lifetime — an in-memory SQLite database vanishes
  when its last connection closes. `Engine/DemoEngineHost.cs` is the shape to copy.
- The engine's `TestEngineHost` is reachable but off limits; keep the demo tests self-contained.

---

### 21. A typed browser storage service to replace the raw `localStorage` interop

**Depends on:** nothing.

**Why.** Two of the first interop calls the demo makes are guaranteed to throw, which is a large share
of the console noise on load and the reason the welcome walkthrough reappears on every visit. Both
sites do `await JS.InvokeAsync<bool>("localStorage.getItem", key)`, and `getItem` returns `string` or
`null`, never a JSON boolean — so `null` and `"true"` both fail to deserialise. A bare `catch { }` then
swallows the exception and defaults to "show it", which hides the failure in C# entirely and would hide
the next interop bug too.

**Scope.**
- `Services/BrowserStorage.cs` plus `wwwroot/js/browserStorage.js`, an ES module loaded with the
  `IJSObjectReference` "import" pattern `Components/EngineCalls/EngineCalls.razor.cs` already uses,
  exposing `GetFlagAsync` / `SetFlagAsync` with a real `bool` on the C# side.
- Rewire both call sites — `Components/FirstVisitWalkthrough.razor` and `Pages/Home.razor` — keeping
  their key strings byte-for-byte so anyone who already dismissed either stays dismissed.
- Replace the blanket `catch { }` with a narrow catch around the interop call only, falling back to the
  safe default. A caught-and-defaulted interop failure is fine; a caught-and-hidden type error is not.
- `BrowserStorage_Tests` over a fake `IJSRuntime`.

**Files.** `src/OpenHoldemEngine.Demo/Services/BrowserStorage.cs`,
`src/OpenHoldemEngine.Demo/wwwroot/js/browserStorage.js`,
`src/OpenHoldemEngine.Demo/Components/FirstVisitWalkthrough.razor`,
`src/OpenHoldemEngine.Demo/Pages/Home.razor`, `src/OpenHoldemEngine.Demo/Program.cs`,
`src/OpenHoldemEngine.Demo.Tests/BrowserStorage_Tests.cs`.

**Definition of done.** No component calls `localStorage.*` directly, and
`grep -rn "InvokeAsync<bool>(\"localStorage" src/OpenHoldemEngine.Demo` is empty.

**Notes for the implementer.**
- The bug is the *type argument* — worth saying so in the commit message, because it is the kind of
  thing that gets rewritten back in.
- Do not add another `window.*` global. The demo has two competing interop styles (globals in
  `index.html` versus collocated ES modules); this task settles on the module style, and task 24
  removes the leftover duplicate global.
- Keep the module specifier relative. Pages serves the demo from the `/OpenHoldemEngine/` sub-path, so
  a leading `/` works locally and 404s in production.
- This is standalone WebAssembly, so there is no prerender phase and interop from `OnInitializedAsync`
  is safe. Do not restructure the components on prerender grounds.
- Do not switch the flags to cookies. `localStorage` is right for a WASM SPA — it does not ride along
  on every HTTP request and has no 4 KB cap — and only the read was broken.

---

### 22. Stop the PHH replay shuffler leaking into the engine options singleton

**Depends on:** nothing.

**Why.** Replaying one imported hand breaks normal play for the rest of the session.
`PhhReplayController.LoadHandAsync` installs its shuffler with
`new HoldemEngineOptionsBuilder().WithCardShuffler(_shuffler)` and never takes it out — and per fact
14 that builder writes the process-wide singleton, so it is global state, not a per-controller setting.
While a shuffler is installed the engine asks it for cards for whatever hand it is dealing, and
`PhhCardShuffler` throws `No deck registered for game …` for anything not registered in advance.
`Unload()` exists and clears the registry, but nobody calls it, and clearing the registry would not
help anyway: the shuffler itself is still installed.

**Scope.**
- Uninstall the shuffler when a replay ends. `HoldemEngineOptions.Instance.CardShuffler = null` is
  reachable from the demo through `InternalsVisibleTo`, and per fact 20 it is the only way to say
  "deal at random again". Keep the set and the clear next to each other so they are visibly symmetric.
- Actually call `Unload()` — from `ReplayControls` on dispose, and from `Home.razor` when the user
  leaves replay for a normal game.
- Wrap `ApplyHoldemActionAsync` in `DemoGameSession` so an escaped exception becomes an `EngineError`
  event rather than a dead page. Reuse the existing error channel; do not add a second one.
- Fix the fire-and-forget `_ = RunAutoPlayAsync(...)` in `ReplayControls`, which catches only
  `OperationCanceledException` and so loses every other failure to an unobserved task.
- A test: replay a hand, unload, then create a **normal** game and start a hand — it must succeed with
  cards that are not the replayed hand's.

**Files.** `src/OpenHoldemEngine.Demo/Services/PhhReplayController.cs`,
`src/OpenHoldemEngine.Demo/Services/DemoGameSession.cs`,
`src/OpenHoldemEngine.Demo/Components/Phh/ReplayControls.razor`,
`src/OpenHoldemEngine.Demo/Pages/Home.razor`,
`src/OpenHoldemEngine.Demo.Tests/PhhReplayLifecycle_Tests.cs`.

**Definition of done.** After an unload, `HoldemEngineOptions.Instance.CardShuffler` is `null` and a
normal game starts and plays.

**Notes for the implementer.**
- Do not change the engine. The product scope allows `InternalsVisibleTo` and nothing else, so widening
  `WithCardShuffler` to accept `null` is out of bounds.
- The tempting alternative — have `PhhCardShuffler` return an empty sequence for an unregistered hand —
  is worse: `CardDeck.Predetermined` with too few cards is a stranger failure, and an unregistered hand
  really is a programming error. Uninstalling is the fix.
- `DemoGameSession` is the only place in the demo that calls `ApplyHoldemActionAsync`. Keep it that way;
  the new `catch` belongs there, not in a component.

---

### 23. Repair the Razor markup that leaks source text into the page

**Depends on:** nothing.

**Why.** Several places render their own source text, or hand a component a C# expression as a literal
string. None of it breaks the build — `Type="AlertType.Info"` is a valid string attribute and text
after `@(...)` is valid markup — so all of it survived the chain while being the first thing a visitor
sees.

**Scope.**
- Add the missing `@` to every enum-valued attribute: eight `AlertType.*` occurrences across
  `Pages/Home.razor`, `Components/Table/GameSetup.razor`, `Components/Phh/HandHistoryImport.razor`,
  `Components/Phh/HandHistoryExport.razor` and `Components/Phh/ReplayControls.razor`. Without it
  AntDesign receives a literal nine-character string and renders an unstyled, icon-less alert. Sweep
  for the same mistake on any other enum attribute.
- `Components/Table/HandHistory.razor`: `@(Pot + StreetPot).ToString("F2")` prints the sum followed by
  the literal text `.ToString("F2")`. Move the call inside the parentheses.
- `Components/Table/GameSetup.razor`: `@(_smallBlind * 2)m` displays a stray `m`, a decimal suffix that
  escaped the expression.
- `Components/Phh/ReplayControls.razor`: `(#{ReplayController.CurrentHand.Index})` is entirely literal,
  because `#{` is not a Razor transition. `PhhHand.Index` does exist, so this only needs a real `@`.
- Delete the dead `Layout/NavMenu.razor` and its `.css` (the untouched template menu, zero references)
  and `wwwroot/sample-data/weather.json`. Keep `wwwroot/sample/sample.phhs`.

**Files.** the six components above, plus the two deletions.

**Definition of done.** `grep -rn 'Type="AlertType' src/OpenHoldemEngine.Demo` is empty, and every
remaining `.ToString(` in a `.razor` file sits inside its `@(...)` rather than after it.

**Notes for the implementer.** This is a sweep, not a redesign: no restyling, no rewording, no layout
changes. There is no analyzer that would have caught any of it, which is why the greps are the
regression guard rather than a test.

---

### 24. Contain panel failures with error boundaries

**Depends on:** 20, 21, 22, 23.

**Why.** There is not one `<ErrorBoundary>` in the demo, so a single panel throwing takes the whole
page down to `An unhandled error has occurred. Reload` with no indication of what broke — which is
exactly what a visitor saw. Its dependencies fix the four causes; this task installs the net, so the
*next* bug degrades one panel instead of the application. For a demo whose only job is to explain the
engine, a generic yellow bar is the wrong failure mode even when nothing is wrong.

**Scope.**
- An `ErrorBoundary` around each of the three regions of `Pages/Home.razor` — table, database
  inspector, engine calls — so one failing leaves the other two usable.
- `ErrorContent` shows the exception's message, not a generic string, with a Retry that calls
  `Recover()`. The demo teaches by showing what actually happened; the same standard applies to its own
  failures.
- Report rather than swallow in `DatabaseInspector`: the `SqliteBrowser` calls in `LoadTableDataAsync`,
  and the discarded `InvokeAsync(async () => …)` in `OnSessionStateChanged`.
- Confirm the `import "./Components/EngineCalls/EngineCalls.razor.js"` specifier resolves under a
  rewritten `<base href="/OpenHoldemEngine/">`, and make it base-relative if it does not.
- Delete the duplicated `scrollToLatestEntry`. It exists both as a module export and as a `window.*`
  global in `index.html`, and only the global is called. Keep the module.

**Files.** `src/OpenHoldemEngine.Demo/Pages/Home.razor`,
`src/OpenHoldemEngine.Demo/Components/Database/DatabaseInspector.razor.cs`,
`src/OpenHoldemEngine.Demo/Components/EngineCalls/EngineCalls.razor.cs` and its `.razor.js`,
`src/OpenHoldemEngine.Demo/wwwroot/index.html`.

**Definition of done.** Each panel is inside its own boundary with a message-bearing `ErrorContent` and
a working `Recover()`, neither `DatabaseInspector` path discards a `Task` silently, and
`grep -n "scrollToLatestEntry" src/OpenHoldemEngine.Demo/wwwroot/index.html` is empty.

**Notes for the implementer.**
- Do not use a boundary to paper over a bug. If a fifth cause turns up while wiring these, fix it or
  open a follow-up — do not let the boundary hide it.
- `ErrorBoundary` catches exceptions from its children's lifecycle methods and event handlers. It does
  **not** catch a `Task` discarded with `_ =`, or a callback running outside the render pipeline, which
  is why the two `DatabaseInspector` paths are named here rather than assumed covered.
- `ErrorBoundary` gives up after `MaximumErrorCount` errors, and `Recover()` is what resets the count —
  so Retry matters functionally, not just cosmetically.
- The stock `#blazor-error-ui` markup stays; it is the last resort for what a boundary cannot catch.
  Note that `app.css` already defines `.blazor-error-boundary` with an `::after` content string that
  will fight a custom message.
- To check the module specifier without a browser, publish, apply the workflow's `sed`, and reason about
  where the file lands relative to the base path. A `./` specifier resolves against the *document* URL,
  not the `<base>` element — that is the subtlety.

---

### 25. Remember the walkthrough and add a header button to reopen it

**Depends on:** 21.

**Why.** The walkthrough is wrong in both directions: it reappears on every visit, because the flag it
writes is never read back successfully, and once dismissed there is no way to bring it back — so a
visitor who clicked Skip has permanently lost the only explanation of the three panels. The header has
room; it holds a title and a GitHub link.

**Scope.**
- Read the dismissed flag through `BrowserStorage` from task 21, keeping the key
  `ohe-demo-walkthrough-dismissed` unchanged so existing dismissals survive.
- A public `Show()` (or a two-way-bound `Visible`) that reopens the modal and resets the step counter to
  1, and `Closable="true"` — `false` is defensible for a first-visit tour but wrong for a modal the user
  opened on purpose.
- A round icon button in `Layout/MainLayout.razor`'s header, next to the GitHub link, with a tooltip,
  that opens it.
- The button and the walkthrough live in different components, so connect them with a shared scoped
  service (a `WalkthroughState` with an event, the pattern `DemoGameSession.StateChanged` already
  establishes) rather than a `@ref` — a layout cannot hold a reference into the page it renders. Moving
  the walkthrough into `MainLayout` is an acceptable alternative if it still fires on a first visit to
  every route.
- Tests for the state, not the rendering: the flag round-trips, and a show request resets the step to 1.

**Files.** `src/OpenHoldemEngine.Demo/Components/FirstVisitWalkthrough.razor`,
`src/OpenHoldemEngine.Demo/Layout/MainLayout.razor` and its `.razor.css`,
`src/OpenHoldemEngine.Demo/Services/WalkthroughState.cs`, `src/OpenHoldemEngine.Demo/Program.cs`,
`src/OpenHoldemEngine.Demo/Pages/Home.razor`, and a test file.

**Definition of done.** The walkthrough shows on a first visit, stays closed after dismissal and a
reload, and reopens at step 1 from the header button.

**Notes for the implementer.**
- Do not reimplement storage; task 21 delivers it. If the read still fails, the bug is in that service
  and belongs there, not in a second copy of the interop.
- Resetting the step matters: without it a reopen lands wherever the user left off, which for someone
  who dismissed at step 3 means an immediate Done button and nothing explained.
- It stays a modal. Do not promote it to a route or redesign the onboarding.
- Every UI string is English, the tooltip included.

---

## Dependency graph

```
1 ─┬─ 2 ─┬─ 3 ─┬─ 5 ── 6 ── 7 ── 8 ──┬── 13
   │      │     │                     │
   │      │     └─ 9 ─────────────────┤
   │      │                           │
   │      ├─ 10                       └── 14
   │      └─ 15
   ├─ 4 ──── 12 ──────────────────────┘
   └─ 11 ──┘

16 needs 3, 11, 14.        17 needs everything.
```

Once tasks 1 to 4 are done, five branches can proceed in parallel: the table chain (5, 6, 7, 8), the
call viewer (9), the database inspector (10), the PHH chain (11, 12), and CI (15).

Phase F is almost flat — six of the eight fixes touch different files and can run in any order:

```
18   19   20 ─┐
              ├─ 24
21 ─┬─────────┤
    │   22 ───┤
    │   23 ───┘
    └─ 25
```

24 needs its four causes fixed first (20, 21, 22, 23); 25 needs the storage service from 21.

## Issue conventions

- Labels: `demo` on every issue, plus one of `blazor`, `phh`, `ci` or `docs`.
- Milestone: `Blazor demo`.
- Every issue repeats the subset of [Engine facts every task depends on](#engine-facts-every-task-depends-on)
  that its task can trip over, so the issue reads on its own without this document.
- Title format matches the headings above, so an issue and its section are easy to line up.
- **Heading numbers are document ordinals, not issue numbers.** Phases A to E map to issues #(N + 2),
  Phase F to issues #(N + 19) — so task 18 is issue #37 and task 25 is issue #44. The
  `**Depends on:**` line uses ordinals in *this document* and real `#` numbers in the *issue body*, and
  only the latter is what `run-next-demo-task.ps1` parses when it picks the next ready task. Getting it
  wrong there does not fail loudly; it just makes the runner skip the task forever.

## End-to-end verification

Run these once the chain is complete.

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
   error bar and **no exceptions in the console**. This one check is what the whole of task 20 exists
   for, and it failed on every run before it.
8. Reload the page with the console open, before touching anything. There must be no `JsonException`
   from a `localStorage` call (task 21).
9. Play a hand to showdown, watching all three panels stay consistent, then start the next hand.
10. Import a hand history, replay it to the end, then start a **normal** game from the same page. The
    normal game must deal random cards and not fail (task 22).
11. Read the page for leaked source text: an alert with no colour or icon, a visible `.ToString("F2")`,
    a stray `m` after the big blind, a literal `(#{...})` next to the replayed hand's name (task 23).
12. Dismiss the walkthrough, reload, and confirm it stays closed. Then reopen it from the header button
    and confirm it starts at step 1 (task 25).
13. Confirm the deployed site at its Pages URL actually loads, including a deep link, once
    `deploy-demo.yml` has run green on `main` (task 18).
