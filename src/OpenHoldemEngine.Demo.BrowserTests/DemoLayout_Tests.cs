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

    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GameSetup_CreateButtonIsItsOwnHitTarget(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);

        IReadOnlyList<HitProbe> probes = await page.ProbeCreateGameButtonAsync();

        probes.Should().HaveCount(1, "Game Setup renders exactly one Create and Start Game button");
        AssertOwnHitTargets(probes);
        AssertNoConsoleErrors(page);
    }

    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_HasNoHorizontalOverflow(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);
        await page.CreateGameAsync();

        ScrollMetrics layoutContent = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);
        ScrollMetrics pageScroller = await page.ReadScrollMetricsAsync(DemoPage.PageScrollerSelector);

        layoutContent.Found.Should().BeTrue($"{DemoPage.LayoutContentSelector} renders the routed page");
        layoutContent.ScrollWidth.Should().Be(
            layoutContent.ClientWidth,
            $"the game page must not overflow horizontally at {viewportWidth}px ({layoutContent})");
        pageScroller.ScrollWidth.Should().Be(
            pageScroller.ClientWidth,
            $"the page must not overflow horizontally at {viewportWidth}px ({pageScroller})");
        AssertNoConsoleErrors(page);
    }

    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_ActionButtonsAreTheirOwnHitTargets(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);
        await page.CreateGameAsync();

        // The action panel sits below the fold in a 900px viewport, so a visitor scrolls to it
        // before clicking. Where the buttons end up is the question; getting there is not.
        await page.ScrollActionPanelIntoViewAsync();

        IReadOnlyList<HitProbe> probes = await page.ProbeHitTargetsAsync(DemoPage.ActionButtonSelectors);

        probes.Should().HaveCountGreaterOrEqualTo(
            MinimumActionButtons,
            "the first seat to act pre-flop is offered Fold, Call and Raise");
        AssertOwnHitTargets(probes);
        AssertNoConsoleErrors(page);
    }

    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_GamePage_ActionDoesNotMoveThePageScroller(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);
        await page.CreateGameAsync();
        await page.ScrollActionPanelIntoViewAsync();

        ScrollMetrics layoutContentBefore = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);
        ScrollMetrics pageScrollerBefore = await page.ReadScrollMetricsAsync(DemoPage.PageScrollerSelector);

        await page.TakeGameActionAsync();

        // Only the horizontal axis is asserted. scrollTop has a legitimate reason to move —
        // Chrome's scroll anchoring adjusts it when content above the viewport grows, and every
        // action grows the page — while nothing should ever move the page sideways.
        ScrollMetrics layoutContentAfter = await page.ReadScrollMetricsAsync(DemoPage.LayoutContentSelector);
        ScrollMetrics pageScrollerAfter = await page.ReadScrollMetricsAsync(DemoPage.PageScrollerSelector);

        layoutContentAfter.ScrollLeft.Should().Be(
            layoutContentBefore.ScrollLeft,
            $"a re-render must not scroll the page sideways (before: {layoutContentBefore}; after: {layoutContentAfter})");
        pageScrollerAfter.ScrollLeft.Should().Be(
            pageScrollerBefore.ScrollLeft,
            $"a re-render must not scroll the page sideways (before: {pageScrollerBefore}; after: {pageScrollerAfter})");
        AssertNoConsoleErrors(page);
    }

    [TestMethod]
    [DataRow(WideViewportWidth)]
    [DataRow(NarrowViewportWidth)]
    public async Task DemoLayout_PlayingAHand_LogsNoConsoleErrors(int viewportWidth)
    {
        await using DemoPage page = await OpenDemoAsync(viewportWidth);
        await page.CreateGameAsync();

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

    private static async Task<DemoPage> OpenDemoAsync(int viewportWidth)
    {
        if (!DemoSite.IsEnabled)
        {
            Assert.Inconclusive(
                "The browser gate is off. Set OHE_TEST_BROWSER_GATE=1 to publish the demo, "
                + "serve it locally and drive it in headless Chromium.");
        }

        DemoSite site = _site ?? throw new InvalidOperationException("The demo site was not started.");

        DemoPage page = await site.NewPageAsync(viewportWidth, ViewportHeight);
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
