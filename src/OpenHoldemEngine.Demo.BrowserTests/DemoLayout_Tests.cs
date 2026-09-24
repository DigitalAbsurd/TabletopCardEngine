using FluentAssertions;

namespace OpenHoldemEngine.Demo.BrowserTests;

/// <summary>
/// The browser gate: is the published demo usable, measured in a real browser.
/// </summary>
/// <remarks>
/// <para>
/// bUnit renders markup and computes no geometry, so element size, stacking order, hit-testing
/// and scroll position are invisible to the other test projects. Every defect of Phase H passed
/// build, format and test with an empty console; the assertions below are what would have
/// failed instead.
/// </para>
/// <para>
/// The assertions run at 1500px and 1280px, because the grid-row defect only appeared below
/// 1366px and the horizontal overflow only mattered above it — one width would have missed one
/// of them.
/// </para>
/// <para>
/// Off by default. Set <c>OHE_TEST_BROWSER_GATE=1</c> to run it; see the project README.
/// </para>
/// </remarks>
[TestClass]
public sealed class DemoLayout_Tests
{
    private const int WideViewportWidth = 1500;
    private const int NarrowViewportWidth = 1280;
    private const int ViewportHeight = 900;

    /// <summary>
    /// A common laptop screen. It is also the breakpoint itself: at exactly 1366px both of the
    /// layout's media queries used to match, which squeezed the table to 769px (issue #104).
    /// </summary>
    private const int LaptopViewportWidth = 1366;

    private const int LaptopViewportHeight = 768;

    /// <summary>Fold, Call and Raise are always offered to the first seat to act pre-flop.</summary>
    private const int MinimumActionButtons = 3;

    private const int MaximumActionsPerHand = 30;

    private static DemoSite? _site;

    [ClassInitialize]
    public static async Task StartDemoSiteAsync(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        if (!DemoSite.IsEnabled)
        {
            return;
        }

        _site = await DemoSite.StartAsync();
    }

    [ClassCleanup]
    public static async Task StopDemoSiteAsync()
    {
        if (_site is not null)
        {
            await _site.DisposeAsync();
            _site = null;
        }
    }

    /// <summary>
    /// The fixture finds the demo by walking up to the solution file; this is the one thing it
    /// assumes about the repository, and the one check that runs whether the gate is on or not.
    /// </summary>
    [TestMethod]
    public void DemoLayout_Fixture_FindsTheDemoProjectToPublish()
    {
        File.Exists(DemoSite.DemoProjectPath).Should().BeTrue(
            $"the browser gate publishes {DemoSite.DemoProjectPath}");
    }

    /// <summary>
    /// The demo has no setup screen: the game page is what loads, and New Game is the only way
    /// back to the settings. Since issue #104 the button is in the page header, beside the session's
    /// summary, so it is hit-tested for the same reason the Create and Start Game button was — an
    /// overlay painted over it would leave a visitor with a running game they cannot reconfigure.
    /// </summary>
    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_NewGameButtonIsItsOwnHitTarget(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);

        IReadOnlyList<HitProbe> probes = await page.ProbeNewGameButtonAsync();

        probes.Should().HaveCount(1, "the settings summary renders exactly one New Game button");
        AssertOwnHitTargets(probes);
        AssertNoConsoleErrors(page);
    }

    /// <summary>
    /// The companions share one column behind a rail of tabs, Visual Studio style: the Database
    /// Inspector is in front when the page opens, each tab is a click target of its own, and a
    /// click shows exactly that companion. The panes are all mounted, so "shown" is measured as a
    /// visible box rather than read from the markup.
    /// </summary>
    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_CompanionTabsShowOnePaneAtATime(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);
        await page.ScrollCompanionTabsIntoViewAsync();

        IReadOnlyList<HitProbe> tabProbes = await page.ProbeHitTargetsAsync(DemoPage.CompanionTabSelector);
        tabProbes.Should().HaveCount(DemoPage.CompanionKeys.Length, "the rail has one tab per companion");
        AssertOwnHitTargets(tabProbes);

        IReadOnlyList<string> visibleOnOpen = await page.ReadVisibleCompanionPanesAsync();
        visibleOnOpen.Should().Equal(
            [DemoPage.DatabaseCompanion],
            "only the Database Inspector is shown when the game page opens");

        foreach (string companion in DemoPage.CompanionKeys.Reverse())
        {
            await page.SelectCompanionTabAsync(companion);

            IReadOnlyList<string> visible = await page.ReadVisibleCompanionPanesAsync();
            visible.Should().Equal([companion], $"selecting the {companion} tab shows that companion alone");

            ScrollMetrics layoutContent = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);
            layoutContent.ScrollWidth.Should().Be(
                layoutContent.ClientWidth,
                $"the {companion} companion must not push the page sideways at {viewportWidth}px ({layoutContent})");
        }

        AssertNoConsoleErrors(page);
    }

    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_HasNoHorizontalOverflow(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);

        ScrollMetrics layoutContent = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);

        layoutContent.Found.Should().BeTrue($"{DemoPage.LayoutContentSelector} renders the routed page");
        layoutContent.ScrollWidth.Should().Be(
            layoutContent.ClientWidth,
            $"the game page must not overflow horizontally at {viewportWidth}px ({layoutContent})");
        AssertNoConsoleErrors(page);
    }

    /// <summary>
    /// The table is the whole game area — seats, street, board, pot and action bar — and it has to
    /// be readable and playable the moment the page opens, without scrolling. Issue #100 folded six
    /// stacked blocks into it because the table alone measured 904px against 836px of scroll
    /// container at 900px high. Issue #104 found that on a 1366x768 laptop, with the first-visit
    /// banner above it, the table measured 758px against 704px and the bar ended at 893px — off the
    /// bottom of the screen — so the bar's own bottom edge is asserted too, with the banner showing.
    /// </summary>
    [TestMethod]
    [DataRow(WideViewportWidth, ViewportHeight)]
    [DataRow(NarrowViewportWidth, ViewportHeight)]
    [DataRow(LaptopViewportWidth, LaptopViewportHeight)]
    public async Task DemoLayout_GamePage_PokerTableFitsTheViewportHeight(int viewportWidth, int viewportHeight)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth, viewportHeight);

        int banners = await page.CountAsync(DemoPage.StorageAlertSelector);
        banners.Should().Be(1, "a first visit shows the in-memory banner above the table, and that is the page measured");

        double tableHeight = await page.ReadElementHeightAsync(DemoPage.PokerTableSelector);
        double barBottom = await page.ReadElementBottomAsync(DemoPage.ActionBarSelector);
        double contentBottom = await page.ReadElementBottomAsync(DemoPage.LayoutContentSelector);
        ScrollMetrics layoutContent = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);

        layoutContent.ScrollTop.Should().Be(0, $"nothing has been scrolled yet ({layoutContent})");
        tableHeight.Should().BeLessOrEqualTo(
            layoutContent.ClientHeight,
            $"the poker table must fit the space under the header at {viewportWidth}x{viewportHeight} "
            + $"(table: {tableHeight}px; {layoutContent})");
        barBottom.Should().BeLessOrEqualTo(
            contentBottom,
            $"the action bar must be on screen when the page opens at {viewportWidth}x{viewportHeight} "
            + $"(bar bottom: {barBottom}px; content bottom: {contentBottom}px)");
        AssertNoConsoleErrors(page);
    }

    /// <summary>
    /// main.layout-content is the page's only scroll container, and the header stays put while
    /// it scrolls.
    /// </summary>
    /// <remarks>
    /// Until issue #72 it was not a scroll container at all: MainLayout's scoped rules are all
    /// ::deep, no element carried the scope attribute, and the document scrolled instead —
    /// taking the header with it while every probe against main.layout-content read zeroes and
    /// looked healthy. Both halves are asserted here: that the container scrolls, and that the
    /// document does not.
    /// <para>
    /// Only the narrow width is taller than the viewport, and so only it can show the container
    /// scrolling: below 1366px the companions stack under the table. Above it, since issue #100,
    /// the table fits the viewport by design and the companion column is bounded beside it.
    /// </para>
    /// </remarks>
    [TestMethod]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_LayoutContentIsTheOnlyScrollContainer(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);

        ScrollMetrics layoutContent = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);
        ScrollMetrics documentScroller = await page.ReadScrollMetricsAsync(DemoPage.DocumentScrollerSelector);

        layoutContent.Overflow.Should().Be(
            "auto",
            $"the routed page's container is what scrolls at {viewportWidth}px ({layoutContent})");
        layoutContent.ScrollHeight.Should().BeGreaterThan(
            layoutContent.ClientHeight,
            $"the game page is taller than the viewport, so there is something to scroll ({layoutContent})");
        documentScroller.ScrollHeight.Should().Be(
            documentScroller.ClientHeight,
            $"the document itself must not scroll at {viewportWidth}px ({documentScroller})");
        documentScroller.ScrollWidth.Should().Be(
            documentScroller.ClientWidth,
            $"the document itself must not overflow sideways at {viewportWidth}px ({documentScroller})");

        double headerTopBefore = await page.ReadElementTopAsync(DemoPage.HeaderSelector);
        await page.ScrollToBottomAsync(DemoPage.LayoutContentSelector);
        ScrollMetrics scrolled = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);
        double headerTopAfter = await page.ReadElementTopAsync(DemoPage.HeaderSelector);

        scrolled.ScrollTop.Should().BeGreaterThan(
            0,
            $"scrolling the container to its bottom must move it ({scrolled})");
        headerTopAfter.Should().Be(
            headerTopBefore,
            "the header must stay in place while the content scrolls under it "
            + $"(before: {headerTopBefore}; after: {headerTopAfter})");
        AssertNoConsoleErrors(page);
    }

    /// <summary>
    /// The half of the scroll-container check that holds at every width: whatever the page's
    /// height, it is main.layout-content that owns the overflow, never the document.
    /// </summary>
    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_DocumentNeverScrolls(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);

        ScrollMetrics layoutContent = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);
        ScrollMetrics documentScroller = await page.ReadScrollMetricsAsync(DemoPage.DocumentScrollerSelector);

        layoutContent.Overflow.Should().Be(
            "auto",
            $"the routed page's container is what scrolls at {viewportWidth}px ({layoutContent})");
        documentScroller.ScrollHeight.Should().Be(
            documentScroller.ClientHeight,
            $"the document itself must not scroll at {viewportWidth}px ({documentScroller})");
        documentScroller.ScrollWidth.Should().Be(
            documentScroller.ClientWidth,
            $"the document itself must not overflow sideways at {viewportWidth}px ({documentScroller})");
        AssertNoConsoleErrors(page);
    }

    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_ActionButtonsAreTheirOwnHitTargets(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);

        // The first-visit storage alert sits above the table and can push the bar's bottom edge
        // past the fold, so the bar is scrolled into view the way a visitor would reach for it.
        // Where the buttons end up is the question; getting there is not.
        await page.ScrollActionPanelIntoViewAsync();

        IReadOnlyList<HitProbe> probes = await page.ProbeHitTargetsAsync(DemoPage.ActionButtonSelectors);

        probes.Should().HaveCountGreaterOrEqualTo(
            MinimumActionButtons,
            "the first seat to act pre-flop is offered Fold, Call and Raise");
        AssertOwnHitTargets(probes);

        IReadOnlyList<HitProbe> quickActions = await page.ProbeHitTargetsAsync(DemoPage.QuickActionsButtonSelector);
        quickActions.Should().HaveCount(1, "the raise presets sit behind one Quick Actions button");
        AssertOwnHitTargets(quickActions);
        AssertNoConsoleErrors(page);
    }

    /// <summary>
    /// The ½ Pot / Pot / All-in presets are one dropdown since issue #104. Its menu is an AntDesign
    /// overlay rendered outside the bar, which bUnit can only see as markup: this is where it is
    /// opened and clicked for real. A preset sizes the raise and submits nothing.
    /// </summary>
    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_QuickActions_AllInSizesTheRaiseWithoutSubmitting(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);

        string raiseBefore = await page.ReadTextAsync(DemoPage.RaiseButtonSelector);
        int callsBefore = await page.CountAsync(DemoPage.EngineCallEntrySelector);

        await page.PickQuickActionAsync("All-in");

        string raiseAfter = await page.ReadTextAsync(DemoPage.RaiseButtonSelector);
        int callsAfter = await page.CountAsync(DemoPage.EngineCallEntrySelector);

        raiseAfter.Should().StartWith("Raise to ");
        raiseAfter.Should().NotBe(raiseBefore, "All-in sizes the raise to the top of the window");
        callsAfter.Should().Be(callsBefore, "a preset only sizes the raise; Raise is what submits it");
        AssertNoConsoleErrors(page);
    }

    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_ActionDoesNotMoveTheScrollContainer(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);
        await page.ScrollActionPanelIntoViewAsync();

        ScrollMetrics before = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);

        await page.TakeGameActionAsync();

        // Only the horizontal axis is asserted. scrollTop has a legitimate reason to move —
        // Chrome's scroll anchoring adjusts it when content above the viewport grows, and every
        // action grows the page — while nothing should ever move the page sideways.
        ScrollMetrics after = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);

        after.ScrollLeft.Should().Be(
            before.ScrollLeft,
            $"a re-render must not scroll the page sideways (before: {before}; after: {after})");
        AssertNoConsoleErrors(page);
    }

    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_PlayingAHand_LogsNoConsoleErrors(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);

        int actions = 0;
        while (actions < MaximumActionsPerHand && await page.TryTakeGameActionAsync())
        {
            actions++;
        }

        actions.Should().BeGreaterThan(0, "the hand must be playable from the action panel");
        actions.Should().BeLessThan(
            MaximumActionsPerHand,
            "the hand must reach its end rather than run out of the action budget");
        AssertNoConsoleErrors(page);
    }

    private static async Task<DemoPage> OpenDemoAsync(int viewportWidth, int viewportHeight = ViewportHeight)
    {
        if (!DemoSite.IsEnabled)
        {
            Assert.Inconclusive(
                "The browser gate is off. Set OHE_TEST_BROWSER_GATE=1 to publish the demo, "
                + "serve it locally and drive it in headless Chromium.");
        }

        DemoSite site = _site ?? throw new InvalidOperationException("The demo site was not started.");

        DemoPage page = await site.NewPageAsync(viewportWidth, viewportHeight);
        await page.OpenAsync();
        return page;
    }

    private static void AssertOwnHitTargets(IReadOnlyList<HitProbe> probes)
    {
        foreach (HitProbe probe in probes)
        {
            probe.Rendered.Should().BeTrue($"the element must have a visible box: {probe}");
            probe.InViewport.Should().BeTrue($"the element's centre must be a point a visitor can click: {probe}");
            probe.IsOwnHitTarget.Should().BeTrue(
                $"a click at the element's centre must reach it rather than something painted over it: {probe}");
        }
    }

    private static void AssertNoConsoleErrors(DemoPage page)
    {
        page.ConsoleErrors.Should().BeEmpty(
            "the browser console must stay clean: " + string.Join(" | ", page.ConsoleErrors));
    }
}
