using Microsoft.Playwright;
using System.Text.Json;

namespace OpenHoldemEngine.Demo.BrowserTests;

/// <summary>
/// One browser tab on the demo, with the probes the layout gate asserts on.
/// </summary>
/// <remarks>
/// Every measurement is taken inside the page with <c>getBoundingClientRect</c> and
/// <c>document.elementFromPoint</c>, so it is the browser's own geometry and hit-testing that
/// answers rather than anything this process computes.
/// </remarks>
internal sealed class DemoPage : IAsyncDisposable
{
    /// <summary>
    /// The element the routed page renders into, and the page's only scroll container.
    /// </summary>
    public const string LayoutContentSelector = "main.layout-content";

    /// <summary>
    /// document.scrollingElement. It scrolled the whole demo until issue #72; now it is the
    /// element that must <em>not</em> scroll, which is what keeps the header in place.
    /// </summary>
    public const string DocumentScrollerSelector = "";

    /// <summary>The header bar, which stays put while the content scrolls underneath it.</summary>
    public const string HeaderSelector = "header.layout-header";

    /// <summary>The action buttons, in the order the action bar renders them.</summary>
    public static readonly string[] ActionButtonSelectors =
        [".action-btn.fold-btn", ".action-btn.check-btn", ".action-btn.call-btn", ".action-btn.raise-btn"];

    /// <summary>
    /// Any action button on the bar along the bottom of the table. The bar renders them only while
    /// a seat is on the clock, so their presence means a hand is waiting for a decision, and their
    /// absence that the hand is over.
    /// </summary>
    public const string ActionBarButtonSelector = ".action-bar .action-btn";

    /// <summary>The table, which holds the whole game area and must fit the viewport's height.</summary>
    public const string PokerTableSelector = ".poker-table";

    /// <summary>
    /// The New Game button on the game page's settings summary. It is the only way into the
    /// settings form now that the demo has no setup screen.
    /// </summary>
    public const string NewGameButtonSelector = ".new-game-btn";

    /// <summary>The tabs on the companion column's rail, one per companion.</summary>
    public const string CompanionTabSelector = ".companion-tab";

    /// <summary>The companion panes behind the rail; all are in the DOM, one is displayed.</summary>
    public const string CompanionPaneSelector = ".companion-pane";

    /// <summary>The companion shown when the game page opens.</summary>
    public const string DatabaseCompanion = "database";

    public const string EngineCallsCompanion = "engine-calls";

    public const string HandHistoryCompanion = "hand-history";

    /// <summary>Every companion key, in the order the rail shows them.</summary>
    public static readonly string[] CompanionKeys = [DatabaseCompanion, EngineCallsCompanion, HandHistoryCompanion];

    private const int BootTimeoutMilliseconds = 120_000;

    /// <summary>
    /// One action produces several re-renders, because several components subscribe to
    /// DemoGameSession.StateChanged. This is how long we let them finish.
    /// </summary>
    private const int SettleMilliseconds = 1_000;

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IBrowserContext _context;
    private readonly IPage _page;
    private readonly string _baseUrl;
    private readonly List<string> _consoleErrors = [];

    public DemoPage(IBrowserContext context, IPage page, string baseUrl)
    {
        _context = context;
        _page = page;
        _baseUrl = baseUrl;

        _page.Console += OnConsoleMessage;
        _page.PageError += OnPageError;
    }

    /// <summary>Console errors and unhandled exceptions seen since the tab was opened.</summary>
    public IReadOnlyList<string> ConsoleErrors => _consoleErrors;

    /// <summary>Loads the demo and waits for the hand it deals on arrival.</summary>
    /// <remarks>
    /// There is nothing to click on the way in any more: the page deals a default game as it loads,
    /// so the boot signal is the first action button on the bar — the only signal that means a hand
    /// is on the clock rather than merely rendered.
    /// </remarks>
    public async Task OpenAsync()
    {
        await _page.GotoAsync(_baseUrl);
        await _page.Locator(ActionBarButtonSelector).First.WaitForAsync(new LocatorWaitForOptions { Timeout = BootTimeoutMilliseconds });
        await _page.WaitForTimeoutAsync(SettleMilliseconds);
    }

    /// <summary>Takes the first available game action and waits for the re-renders it causes.</summary>
    public async Task TakeGameActionAsync()
    {
        bool taken = await TryTakeGameActionAsync();
        if (!taken)
        {
            throw new InvalidOperationException("The action panel offered no action to take.");
        }
    }

    /// <summary>
    /// Takes the first available game action on the action bar, or reports that the bar offers
    /// none — which is how the hand ends: a finished hand's bar shows Next Hand instead.
    /// </summary>
    /// <remarks>
    /// The Engine Calls pane is hidden while another companion tab is in front, so its tab is
    /// selected before the action, and before waiting for the new entry to be visible.
    /// </remarks>
    public async Task<bool> TryTakeGameActionAsync()
    {
        ILocator buttons = _page.Locator(ActionBarButtonSelector);
        if (await buttons.CountAsync() == 0)
        {
            return false;
        }

        await SelectCompanionTabAsync(EngineCallsCompanion);

        ILocator entries = _page.Locator(".engine-call-entry");
        int before = await entries.CountAsync();

        await buttons.First.ClickAsync();

        await entries.Nth(before).WaitForAsync(new LocatorWaitForOptions { Timeout = BootTimeoutMilliseconds });
        await _page.WaitForTimeoutAsync(SettleMilliseconds);
        return true;
    }

    /// <summary>
    /// Clicks one companion's tab on the rail, unless it is already the selected one, and waits for
    /// the re-render.
    /// </summary>
    public async Task SelectCompanionTabAsync(string companion)
    {
        ILocator tab = _page.Locator($"{CompanionTabSelector}[data-companion=\"{companion}\"]");
        if (await tab.GetAttributeAsync("aria-selected") == "true")
        {
            return;
        }

        await tab.ClickAsync();
        await _page.WaitForTimeoutAsync(SettleMilliseconds);
    }

    /// <summary>
    /// The <c>data-companion</c> key of every companion pane that currently has a visible box.
    /// </summary>
    public async Task<IReadOnlyList<string>> ReadVisibleCompanionPanesAsync()
    {
        string json = await _page.EvaluateAsync<string>(
            """
            (selector) => JSON.stringify(
                Array.from(document.querySelectorAll(selector))
                    .filter(pane => {
                        const rect = pane.getBoundingClientRect();
                        return rect.width > 0 && rect.height > 0;
                    })
                    .map(pane => pane.dataset.companion))
            """,
            CompanionPaneSelector);

        return JsonSerializer.Deserialize<string[]>(json, _jsonOptions)
            ?? throw new InvalidOperationException("The companion pane probe returned nothing.");
    }

    /// <summary>Scrolls the companion tab rail into view; below 1366px it sits under the table.</summary>
    public async Task ScrollCompanionTabsIntoViewAsync()
    {
        await _page.Locator(".companion-tab-rail").ScrollIntoViewIfNeededAsync();
        await _page.WaitForTimeoutAsync(SettleMilliseconds);
    }

    /// <summary>Scrolls the action panel into view, the way a visitor reaching for a button would.</summary>
    public async Task ScrollActionPanelIntoViewAsync()
    {
        await _page.Locator(".action-panel").ScrollIntoViewIfNeededAsync();
        await _page.WaitForTimeoutAsync(SettleMilliseconds);
    }

    /// <summary>
    /// Reads the computed overflow, scroll offsets and sizes of one element, or of the document
    /// scroller when the selector is <see cref="DocumentScrollerSelector"/>.
    /// </summary>
    public async Task<ScrollMetrics> ReadScrollMetricsAsync(string selector)
    {
        string json = await _page.EvaluateAsync<string>(
            """
            (selector) => {
                const element = selector === '' ? document.scrollingElement : document.querySelector(selector);
                if (element === null) {
                    return JSON.stringify({ found: false });
                }

                return JSON.stringify({
                    found: true,
                    overflow: window.getComputedStyle(element).overflow,
                    scrollLeft: element.scrollLeft,
                    scrollTop: element.scrollTop,
                    scrollWidth: element.scrollWidth,
                    clientWidth: element.clientWidth,
                    scrollHeight: element.scrollHeight,
                    clientHeight: element.clientHeight,
                });
            }
            """,
            selector);

        ScrollMetrics metrics = JsonSerializer.Deserialize<ScrollMetrics>(json, _jsonOptions)
            ?? throw new InvalidOperationException("The scroll probe returned nothing.");

        return metrics with { Selector = selector };
    }

    /// <summary>Reads the rendered height of one element.</summary>
    public async Task<double> ReadElementHeightAsync(string selector)
    {
        double? height = await _page.EvaluateAsync<double?>(
            """
            (selector) => {
                const element = document.querySelector(selector);
                return element === null ? null : element.getBoundingClientRect().height;
            }
            """,
            selector);

        return height ?? throw new InvalidOperationException($"No element matched {selector}.");
    }

    /// <summary>Reads the viewport-relative top edge of one element.</summary>
    public async Task<double> ReadElementTopAsync(string selector)
    {
        double? top = await _page.EvaluateAsync<double?>(
            """
            (selector) => {
                const element = document.querySelector(selector);
                return element === null ? null : element.getBoundingClientRect().top;
            }
            """,
            selector);

        return top ?? throw new InvalidOperationException($"No element matched {selector}.");
    }

    /// <summary>
    /// Scrolls one element to its bottom, the way a visitor reading down the page does, and
    /// waits for the re-renders that follow.
    /// </summary>
    public async Task ScrollToBottomAsync(string selector)
    {
        await _page.EvaluateAsync(
            """
            (selector) => {
                const element = selector === '' ? document.scrollingElement : document.querySelector(selector);
                if (element !== null) {
                    element.scrollTop = element.scrollHeight;
                }
            }
            """,
            selector);
        await _page.WaitForTimeoutAsync(SettleMilliseconds);
    }

    /// <summary>Hit-tests the centre of every element matching one of the given selectors.</summary>
    public Task<IReadOnlyList<HitProbe>> ProbeHitTargetsAsync(params string[] selectors)
    {
        return ProbeAsync(selectors, textPrefix: null);
    }

    /// <summary>Hit-tests the centre of every button whose label starts with the given text.</summary>
    public Task<IReadOnlyList<HitProbe>> ProbeButtonByLabelAsync(string textPrefix)
    {
        return ProbeAsync(["button"], textPrefix);
    }

    /// <summary>Hit-tests the New Game button on the game page's settings summary.</summary>
    public Task<IReadOnlyList<HitProbe>> ProbeNewGameButtonAsync()
    {
        return ProbeHitTargetsAsync(NewGameButtonSelector);
    }

    public async ValueTask DisposeAsync()
    {
        _page.Console -= OnConsoleMessage;
        _page.PageError -= OnPageError;
        await _context.CloseAsync();
    }

    private async Task<IReadOnlyList<HitProbe>> ProbeAsync(string[] selectors, string? textPrefix)
    {
        string json = await _page.EvaluateAsync<string>(
            """
            (request) => {
                const describe = (element) => {
                    if (element === null) {
                        return 'nothing';
                    }

                    const classes = typeof element.className === 'string' && element.className.trim().length > 0
                        ? '.' + element.className.trim().split(/\s+/).join('.')
                        : '';
                    return element.tagName.toLowerCase() + classes;
                };

                const probes = [];
                for (const selector of request.selectors) {
                    for (const element of document.querySelectorAll(selector)) {
                        const label = (element.innerText || '').trim();
                        if (request.textPrefix !== null && !label.startsWith(request.textPrefix)) {
                            continue;
                        }

                        const rect = element.getBoundingClientRect();
                        const style = window.getComputedStyle(element);
                        const rendered = rect.width > 0 && rect.height > 0
                            && style.display !== 'none' && style.visibility !== 'hidden';
                        const centreX = rect.left + rect.width / 2;
                        const centreY = rect.top + rect.height / 2;
                        const inViewport = centreX >= 0 && centreY >= 0
                            && centreX < window.innerWidth && centreY < window.innerHeight;
                        const hit = rendered && inViewport ? document.elementFromPoint(centreX, centreY) : null;

                        probes.push({
                            selector: selector,
                            label: label,
                            rendered: rendered,
                            inViewport: inViewport,
                            left: rect.left,
                            top: rect.top,
                            width: rect.width,
                            height: rect.height,
                            centreX: centreX,
                            centreY: centreY,
                            isOwnHitTarget: hit !== null && (hit === element || element.contains(hit)),
                            hitDescription: describe(hit),
                        });
                    }
                }

                return JSON.stringify(probes);
            }
            """,
            new { selectors, textPrefix });

        return JsonSerializer.Deserialize<HitProbe[]>(json, _jsonOptions)
            ?? throw new InvalidOperationException("The hit-test probe returned nothing.");
    }

    private void OnConsoleMessage(object? sender, IConsoleMessage message)
    {
        if (string.Equals(message.Type, "error", StringComparison.Ordinal))
        {
            _consoleErrors.Add($"console.error: {message.Text}");
        }
    }

    private void OnPageError(object? sender, string error)
    {
        _consoleErrors.Add($"page error: {error}");
    }
}
