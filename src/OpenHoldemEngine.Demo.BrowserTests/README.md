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

1. **No horizontal overflow** — `scrollWidth === clientWidth`, on `main.layout-content` and on
   `document.scrollingElement`.
2. **Every action button is its own hit target** — `document.elementFromPoint` at the centre of each
   of Fold / Check / Call / Raise returns that button or a descendant of it. Same for
   `Create and Start Game` on `Game Setup`.
3. **A render does not move the page scroller sideways** — `scrollLeft` before an action equals
   `scrollLeft` after it.
4. **No console errors** — during load, during game creation, and while a hand is played out.

## What it does not cover

- **Appearance.** There are no screenshot or HTML-diff comparisons, on purpose: a pixel or markup
  snapshot fails on every styling change and teaches the next contributor to delete the test.
- **Vertical scroll position.** Chrome's scroll anchoring legitimately adjusts `scrollTop` when
  content above the viewport grows, and every action grows the page. The horizontal axis, which has
  no legitimate reason to move, is the one that is asserted.
- **Anything below 1280px**, other browsers, and the deployed GitHub Pages site with its rewritten
  `<base href>`.
- **Poker.** Correctness of the engine belongs to `OpenHoldemEngine.Tests`; this gate plays a hand
  only to have something to measure.

## Running it

The gate is **off by default**, because the first run downloads a browser and publishes the
WebAssembly app. `dotnet test` at the repository root reports its ten browser checks as skipped and
runs only `DemoLayout_Fixture_FindsTheDemoProjectToPublish`, which needs no browser.

```bash
# once: install the browser (the fixture also does this, with the same effect)
pwsh src/OpenHoldemEngine.Demo.BrowserTests/bin/Debug/net8.0/playwright.ps1 install --with-deps chromium

# the gate itself
OHE_TEST_BROWSER_GATE=1 dotnet test src/OpenHoldemEngine.Demo.BrowserTests
```

On Windows PowerShell, `$env:OHE_TEST_BROWSER_GATE = "1"` sets the variable.

A run takes about **two minutes**: roughly one minute to `dotnet publish -c Release` the demo (the
`wasm-tools` workload is required) and about one minute for the ten browser checks, plus the browser
download on a cold machine.

Environment variables:

| Variable | Effect |
|---|---|
| `OHE_TEST_BROWSER_GATE` | `1` or `true` runs the gate. Anything else skips it. |
| `OHE_TEST_BROWSER_GATE_PUBLISH_DIR` | Publish into this directory and reuse it when it already holds a `wwwroot/index.html`. Speeds up iteration on the tests themselves; **stale artefacts make the gate lie**, so delete it after changing the demo. |

To run one check:

```bash
OHE_TEST_BROWSER_GATE=1 dotnet run --project src/OpenHoldemEngine.Demo.BrowserTests -- --filter "FullyQualifiedName~HasNoHorizontalOverflow"
```

CI runs the gate as its own step in [ci.yml](../../.github/workflows/ci.yml), after the ordinary
`dotnet test`.
