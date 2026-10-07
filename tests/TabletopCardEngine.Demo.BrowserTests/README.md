# Browser layout gate

A headless-browser smoke gate over the **published** demo: is the page usable, measured in a real
browser rather than in a renderer that computes no geometry.

`dotnet build`, `dotnet format --verify-no-changes` and `dotnet test` were all green — with an empty
browser console — while the demo's game page could not be clicked, scrolled or read. bUnit renders
markup and computes no layout, so element size, stacking order, hit-testing and scroll position are
invisible to it. This project is the layer that can see them.

## What it asserts

On `Game Setup` and on the game page, at **1500px and 1280px** wide (the grid-row defect only
appeared below 1366px and the horizontal overflow only mattered above it):

1. **No horizontal overflow** — `scrollWidth === clientWidth` on `main.layout-content`.
2. **`main.layout-content` is the page's only scroll container** — at both widths its computed
   `overflow` is `auto` and `document.scrollingElement` neither scrolls nor overflows. At 1280×560,
   where the table is taller than the space under the tab bar, the container is also scrolled to its
   bottom and the header must not move. Before issue #72 none of MainLayout's scoped rules applied,
   so the document scrolled and took the header with it while every probe against
   `main.layout-content` read zeroes. Since issue #106 every pane fits a 900px-high screen, so the
   scrolling half needs the short viewport.
3. **The poker table fits the viewport's height** — `.poker-table` is no taller than
   `main.layout-content`'s `clientHeight`, and the bottom edge of `.action-bar` is above the
   container's bottom edge, at 1500×900, 1280×900 and 1366×768 with nothing scrolled. Issue #104 added the laptop size and the bar's edge: at 1366×768 the table
   measured 758px against 704px and the bar ended off the bottom of the screen. Issue #100 had
   folded the street, the game summary, the hand's outcome and the actions onto the table because
   the table alone measured 904px against 836px of container; #104 moved the summary on into the
   page header.
4. **Every action button is its own hit target** — `document.elementFromPoint` at the centre of
   each returns that element or a descendant of it. Since issue #100 the actions sit on an inline
   action bar along the bottom of the table (`.action-bar`), rendered only while a seat is on the
   clock; the Drawer and its trigger of issue #80 are gone.
5. **A render does not move the scroll container sideways** — `scrollLeft` before an action equals
   `scrollLeft` after it.
6. **No console errors** — during load, during game creation, and while a hand is played out. Each
   action taken by the gate clicks a button on the action bar, exactly as a visitor would; the hand
   is over when the bar offers Next Hand instead.
7. **The raise slider's marks and the pot presets size the raise and submit nothing** — Min and
   All-in are marks on the AntDesign raise slider (issue #112), and since issue #114 ½ Pot and Pot
   are white buttons stacked beside the amount box; the Quick Actions dropdown must be gone. Each
   mark, preset and the box itself is its own hit target. The gate clicks All-in and checks that
   the Raise button's amount changed while the engine-call log did not grow, clicks Pot, then
   clicks Min and checks that the amount is exactly the minimum again. It also types an illegal
   amount and checks that Raise is disabled until the round fix button beside the box corrects it.
7a. **The header's theme switch** (issue #114) flips `<html data-theme>` and AntDesign's stylesheet,
   survives a reload, and leaves no horizontal overflow; the header carries the short commit SHA.
8. **Tabs** — since issue #106 the table and its companions are four tabs on one horizontal bar
   under the header. Each tab is its own hit target, Game Table is the only pane shown on open, a
   click shows exactly that pane, and no pane pushes the page sideways.
9. **Every pane fills the screen** — at 1500×900 and 1366×768 each tab's pane reaches the bottom of
   `main.layout-content`, and the green table stretches with it. Before #106 the table ended at
   660px of a 900px screen.
10. **The storage warning is a notification** — an AntDesign notification below the tab bar, not a
    banner above the table. The gate closes it on open everywhere else, so its overlay cannot answer
    a hit-test meant for the page.
11. **The action bar lines up with the seats** — since issue #108 .action-bar's left and right
    edges match the bottom .table-seats-row's, at 1500px and 1280px. Before, it stretched across
    the whole table: 1444px against the rows' 1200px at 1500px.
12. **Copy follows the status in the call log** — in an .engine-call-entry, .copy-btn starts
    within one header gap of the ✓ Success / ✗ Failed text rather than at the far right edge.
13. **The Hand History cards stop at 800px** — Import, Replay Controls (once a hand is picked) and
    Export are at most 800px wide and stay on the pane's left edge.
14. **The New Game form does not overlap itself** — since issue #112 the gate opens the New Game
    modal and checks that no text field's box intersects any text in the form, and that
    "+ Add Player" is its own hit target. The probe finds fields and text without class names, so it
    reads the old markup too. There the Seat input ran into the "Buy-In:" caption, and the Engine
    Limitations alert sat over the button.

## What it does not cover

- **Appearance.** There are no screenshot or HTML-diff comparisons, on purpose: a pixel or markup
  snapshot fails on every styling change and teaches the next contributor to delete the test.
- **Vertical scroll position.** Chrome's scroll anchoring legitimately adjusts `scrollTop` when
  content above the viewport grows, and every action grows the page. The horizontal axis, which has
  no legitimate reason to move, is the one that is asserted.
- **Anything below 1280px**, other browsers, and the deployed GitHub Pages site with its rewritten
  `<base href>`.
- **Poker.** Correctness of the engine belongs to `TabletopCardEngine.Tests`; this gate plays a hand
  only to have something to measure.

## Running it

The gate is **off by default**, because the first run downloads a browser and publishes the
WebAssembly app. `dotnet test` at the repository root reports its twenty-seven browser checks as skipped
and runs only `DemoLayout_Fixture_FindsTheDemoProjectToPublish`, which needs no browser.

```bash
# once: install the browser (the fixture also does this, with the same effect)
pwsh tests/TabletopCardEngine.Demo.BrowserTests/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium

# the gate itself
OHE_TEST_BROWSER_GATE=1 dotnet test tests/TabletopCardEngine.Demo.BrowserTests
```

On Windows PowerShell, `$env:OHE_TEST_BROWSER_GATE = "1"` sets the variable.

A run takes about **two minutes**: roughly one minute to `dotnet publish -c Release` the demo (the
`wasm-tools` workload is required) and about three minutes for the thirty-four browser checks, plus the
browser download on a cold machine.

Environment variables:

| Variable | Effect |
|---|---|
| `OHE_TEST_BROWSER_GATE` | `1` or `true` runs the gate. Anything else skips it. |
| `OHE_TEST_BROWSER_GATE_PUBLISH_DIR` | Publish into this directory and reuse it when it already holds a `wwwroot/index.html`. Speeds up iteration on the tests themselves; **stale artefacts make the gate lie**, so delete it after changing the demo. |

To run one check:

```bash
OHE_TEST_BROWSER_GATE=1 dotnet run --project tests/TabletopCardEngine.Demo.BrowserTests -- --filter "FullyQualifiedName~HasNoHorizontalOverflow"
```

CI runs the gate as its own step in [ci.yml](../../.github/workflows/ci.yml), after the ordinary
`dotnet test`.
