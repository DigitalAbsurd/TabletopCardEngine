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

    /// <summary>
    /// Short enough that the table's own content is taller than the space under the tab bar, so
    /// main.layout-content has something to scroll. Since issue #106 nothing stacks under the table
    /// any more, and at 1280x900 the page fits.
    /// </summary>
    private const int ShortViewportHeight = 560;

    /// <summary>Sub-pixel rounding between the pane's and the container's edges.</summary>
    private const double EdgeTolerancePixels = 1;

    /// <summary>
    /// How far above the container's bottom edge the table may end: the pane's own bottom padding,
    /// with room to spare, and far less than the 240px band the table left before issue #106.
    /// </summary>
    private const double PanePaddingAllowancePixels = 32;

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
    /// The game table and its companions are tabs on one horizontal bar under the header: Game
    /// Table is in front when the page opens, each tab is a click target of its own, and a click
    /// shows exactly that pane. The panes are all mounted, so "shown" is measured as a visible box
    /// rather than read from the markup.
    /// </summary>
    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_TabsShowOnePaneAtATime(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);

        IReadOnlyList<HitProbe> tabProbes = await page.ProbeHitTargetsAsync(DemoPage.TabSelector);
        tabProbes.Should().HaveCount(DemoPage.TabKeys.Length, "the bar has one tab per pane");
        AssertOwnHitTargets(tabProbes);

        double headerBottom = await page.ReadElementBottomAsync(DemoPage.HeaderSelector);
        foreach (HitProbe tab in tabProbes)
        {
            tab.Top.Should().BeGreaterOrEqualTo(headerBottom, $"the tabs sit under the header: {tab}");
            tab.Top.Should().Be(tabProbes[0].Top, $"the tabs are one horizontal row: {tab}");
        }

        IReadOnlyList<string> visibleOnOpen = await page.ReadVisibleTabPanesAsync();
        visibleOnOpen.Should().Equal(
            [DemoPage.GameTableTab],
            "only the Game Table is shown when the game page opens");

        foreach (string tabKey in DemoPage.TabKeys.Reverse())
        {
            await page.SelectTabAsync(tabKey);

            IReadOnlyList<string> visible = await page.ReadVisibleTabPanesAsync();
            visible.Should().Equal([tabKey], $"selecting the {tabKey} tab shows that pane alone");

            ScrollMetrics layoutContent = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);
            layoutContent.ScrollWidth.Should().Be(
                layoutContent.ClientWidth,
                $"the {tabKey} pane must not push the page sideways at {viewportWidth}px ({layoutContent})");
        }

        AssertNoConsoleErrors(page);
    }

    /// <summary>
    /// Every tab's pane is at least as tall as the space under the header and the tab bar. Before
    /// issue #106 the table ended at 660px of a 900px screen and the companion column at 754px,
    /// leaving an empty band under both.
    /// </summary>
    [TestMethod]
    [DataRow(WideViewportWidth, ViewportHeight)]
    [DataRow(LaptopViewportWidth, LaptopViewportHeight)]
    public async Task DemoLayout_GamePage_ActivePaneFillsTheViewportHeight(int viewportWidth, int viewportHeight)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth, viewportHeight);

        foreach (string tabKey in DemoPage.TabKeys)
        {
            await page.SelectTabAsync(tabKey);

            double paneBottom = await page.ReadElementBottomAsync(DemoPage.ActiveTabPaneSelector);
            double contentBottom = await page.ReadElementBottomAsync(DemoPage.LayoutContentSelector);
            ScrollMetrics layoutContent = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);

            paneBottom.Should().BeGreaterOrEqualTo(
                contentBottom - EdgeTolerancePixels,
                $"the {tabKey} pane must reach the bottom of the screen at {viewportWidth}x{viewportHeight} "
                + $"(pane bottom: {paneBottom}px; content bottom: {contentBottom}px)");
            layoutContent.ScrollWidth.Should().Be(
                layoutContent.ClientWidth,
                $"the {tabKey} pane must not push the page sideways at {viewportWidth}px ({layoutContent})");
        }

        await page.SelectTabAsync(DemoPage.GameTableTab);
        double tableBottom = await page.ReadElementBottomAsync(DemoPage.PokerTableSelector);
        double gameContentBottom = await page.ReadElementBottomAsync(DemoPage.LayoutContentSelector);
        tableBottom.Should().BeGreaterThan(
            gameContentBottom - PanePaddingAllowancePixels,
            "the green table itself stretches to the bottom of its pane, not only the pane behind it "
            + $"(table bottom: {tableBottom}px; content bottom: {gameContentBottom}px)");
        AssertNoConsoleErrors(page);
    }

    /// <summary>
    /// The "in-memory only" warning is an AntDesign notification since issue #106, not a banner
    /// above the table: it is shown on load, takes no height from the page, and closes itself.
    /// </summary>
    [TestMethod]
    [DataRow(WideViewportWidth)]
    public async Task DemoLayout_GamePage_StorageWarningIsANotification(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth, keepNotification: true);

        int notices = await page.CountAsync(DemoPage.StorageNoticeSelector);
        int banners = await page.CountAsync(DemoPage.StorageAlertSelector);

        notices.Should().Be(1, "every load opens the in-memory notification");
        banners.Should().Be(0, "the warning no longer takes a line above the table");

        double noticeTop = await page.ReadElementTopAsync(DemoPage.StorageNoticeSelector);
        double tabBarBottom = await page.ReadElementBottomAsync(".demo-tab-bar");
        noticeTop.Should().BeGreaterOrEqualTo(
            tabBarBottom,
            "the notification opens below the header and the tabs, so it covers neither New Game nor a tab");
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
    /// bottom of the screen — so the bar's own bottom edge is asserted too. Since issue #106 the banner
    /// is a notification in an overlay, so it no longer takes height from the table.
    /// </summary>
    [TestMethod]
    [DataRow(WideViewportWidth, ViewportHeight)]
    [DataRow(NarrowViewportWidth, ViewportHeight)]
    [DataRow(LaptopViewportWidth, LaptopViewportHeight)]
    public async Task DemoLayout_GamePage_PokerTableFitsTheViewportHeight(int viewportWidth, int viewportHeight)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth, viewportHeight);

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
    /// Since issue #106 every tab's pane fits a 900px-high screen by design, so the check runs on
    /// a short one, where the table's own content is taller than the space under the tab bar.
    /// </para>
    /// </remarks>
    [TestMethod]
    [DataRow(NarrowViewportWidth, ShortViewportHeight)]
    public async Task DemoLayout_GamePage_LayoutContentIsTheOnlyScrollContainer(int viewportWidth, int viewportHeight)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth, viewportHeight);

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

        // The bar is scrolled into view the way a visitor would reach for it. Where the buttons
        // end up is the question; getting there is not.
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

    private static async Task<DemoPage> OpenDemoAsync(
        int viewportWidth,
        int viewportHeight = ViewportHeight,
        bool keepNotification = false)
    {
        if (!DemoSite.IsEnabled)
        {
            Assert.Inconclusive(
                "The browser gate is off. Set OHE_TEST_BROWSER_GATE=1 to publish the demo, "
                + "serve it locally and drive it in headless Chromium.");
        }

        DemoSite site = _site ?? throw new InvalidOperationException("The demo site was not started.");

        DemoPage page = await site.NewPageAsync(viewportWidth, viewportHeight);
        await page.OpenAsync(keepNotification);
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
