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

    /// <summary>The action buttons, in the order the action panel renders them.</summary>
    public static readonly string[] ActionButtonSelectors =
        [".action-btn.fold-btn", ".action-btn.check-btn", ".action-btn.call-btn", ".action-btn.raise-btn"];

    /// <summary>
    /// The trigger button that opens the action Drawer. It exists only when the seat to act has
    /// legal actions — the same condition that used to gate <c>.action-panel .primary-actions</c>.
    /// </summary>
    public const string ActionTriggerSelector = ".action-trigger-btn";

    /// <summary>The Drawer's open state, set by AntDesign once its opening transition starts.</summary>
    public const string ActionDrawerOpenSelector = ".ant-drawer-open";

    /// <summary>
    /// The New Game button on the game page's settings summary. It is the only way into the
    /// settings form now that the demo has no setup screen.
    /// </summary>
    public const string NewGameButtonSelector = ".new-game-btn";

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
    /// so the boot signal is the action trigger — the same one that used to mean "the game this
    /// test just created is running", and the only signal that means a hand is on the clock rather
    /// than merely rendered.
    /// </remarks>
    public async Task OpenAsync()
    {
        await _page.GotoAsync(_baseUrl);
        await _page.Locator(ActionTriggerSelector).WaitForAsync(new LocatorWaitForOptions { Timeout = BootTimeoutMilliseconds });
        await _page.WaitForTimeoutAsync(SettleMilliseconds);
    }

    /// <summary>
    /// Opens the action Drawer by clicking its trigger, and waits for it to finish opening.
    /// A no-op if it is already open — the trigger stays in the DOM behind an open Drawer and a
    /// click on it there would hit the Drawer's own body instead.
    /// </summary>
    public async Task OpenActionDrawerAsync()
    {
        if (await _page.Locator(ActionDrawerOpenSelector).CountAsync() > 0)
        {
            return;
        }

        await _page.Locator(ActionTriggerSelector).ClickAsync();
        await _page.Locator(ActionDrawerOpenSelector).WaitForAsync(new LocatorWaitForOptions { Timeout = BootTimeoutMilliseconds });
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
    /// Opens the Drawer and takes the first available game action, or reports that the trigger
    /// is absent — which is how the hand ends.
    /// </summary>
    /// <remarks>
    /// Checking for the trigger rather than the action buttons themselves matters: the buttons
    /// are always present in the DOM once a hand has offered them at least once (AntDesign's
    /// Drawer renders its ChildContent unconditionally and only moves it off-screen when
    /// closed), so counting them would report actions as available even with a closed Drawer,
    /// and clicking one at that point would silently miss — it sits outside the viewport.
    /// The Engine Calls panel may be collapsed, so it is expanded before opening the Drawer,
    /// and before waiting for the new entry to be visible.
    /// </remarks>
    public async Task<bool> TryTakeGameActionAsync()
    {
        ILocator trigger = _page.Locator(ActionTriggerSelector);
        if (await trigger.CountAsync() == 0)
        {
            return false;
        }

        // Expand the Engine Calls panel before opening the drawer, so the new entry will be visible
        // after the action is taken
        ILocator engineCallsPanel = _page.Locator(".ant-collapse-item").Filter(new LocatorFilterOptions { HasText = "Engine Calls" });
        if (await engineCallsPanel.CountAsync() > 0)
        {
            ILocator panelItem = engineCallsPanel.Nth(0);
            ILocator panelHeader = panelItem.Locator(".ant-collapse-header");
            if (await panelHeader.CountAsync() > 0)
            {
                // Check if the panel is inactive (collapsed) by checking for the active class
                string? classList = await _page.EvaluateAsync<string>(
                    "el => el.className",
                    await panelItem.ElementHandleAsync());
                if (classList != null && !classList.Contains("ant-collapse-item-active"))
                {
                    await panelHeader.ClickAsync();
                    await _page.WaitForTimeoutAsync(SettleMilliseconds);
                }
            }
        }

        await OpenActionDrawerAsync();

        ILocator buttons = _page.Locator(string.Join(", ", ActionButtonSelectors));
        if (await buttons.CountAsync() == 0)
        {
            return false;
        }

        ILocator entries = _page.Locator(".engine-call-entry");
        int before = await entries.CountAsync();

        await buttons.First.ClickAsync();

        await entries.Nth(before).WaitForAsync(new LocatorWaitForOptions { Timeout = BootTimeoutMilliseconds });
        await _page.WaitForTimeoutAsync(SettleMilliseconds);
        return true;
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
