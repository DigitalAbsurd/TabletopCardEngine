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
    /// The element the routed page renders into. It is meant to be the page's only scroll
    /// container; in the published app it is not one, so both it and the page scroller below
    /// are measured.
    /// </summary>
    public const string LayoutContentSelector = "main.layout-content";

    /// <summary>document.scrollingElement, which is what actually scrolls the demo today.</summary>
    public const string PageScrollerSelector = "";

    /// <summary>The action buttons, in the order the action panel renders them.</summary>
    public static readonly string[] ActionButtonSelectors =
        [".action-btn.fold-btn", ".action-btn.check-btn", ".action-btn.call-btn", ".action-btn.raise-btn"];

    private const string CreateGameLabel = "Create and Start Game";
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

    /// <summary>Loads the demo and waits for the WebAssembly runtime to render Game Setup.</summary>
    public async Task OpenAsync()
    {
        await _page.GotoAsync(_baseUrl);
        await _page.Locator(".game-setup-card").WaitForAsync(new LocatorWaitForOptions { Timeout = BootTimeoutMilliseconds });
        await _page.WaitForTimeoutAsync(SettleMilliseconds);
    }

    /// <summary>Creates the pre-filled game and waits for the action panel of the first hand.</summary>
    public async Task CreateGameAsync()
    {
        await _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = CreateGameLabel }).ClickAsync();
        await _page.Locator(".action-panel .primary-actions").WaitForAsync(new LocatorWaitForOptions { Timeout = BootTimeoutMilliseconds });
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
    /// Takes the first available game action, or reports that the action panel offers none —
    /// which is how the hand ends.
    /// </summary>
    public async Task<bool> TryTakeGameActionAsync()
    {
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
    /// Reads the scroll offsets and sizes of one element, or of the page scroller when the
    /// selector is <see cref="PageScrollerSelector"/>.
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

    /// <summary>Hit-tests the Create and Start Game button of Game Setup.</summary>
    public Task<IReadOnlyList<HitProbe>> ProbeCreateGameButtonAsync()
    {
        return ProbeButtonByLabelAsync(CreateGameLabel);
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
