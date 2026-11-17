using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Demo.Components.Table;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Demo.Tests;

[TestClass]
public sealed class SeatMenu_Tests
{
    private static SeatData Seat(decimal stack = 1000m, bool hasLeft = false, decimal pending = 0m, int rebuys = 0)
        => new(SeatNo: 6, Name: "Player 6", StartHandStack: 1000m, Stack: stack) { HasLeft = hasLeft, PendingBuyIn = pending, Rebuys = rebuys };

    private static GameOptionData Options(bool topUp = false, bool rebuy = false, int? maxRebuys = null)
        => new() { AllowTopUp = topUp, AllowRebuy = rebuy, MaxRebuys = maxRebuys };

    [TestMethod]
    public void GetItemStates_BetweenHandsWithTopUpAndRebuyAllowed_OffersTopUpToASeatWithChips()
    {
        var states = SeatMenu.GetItemStates(Seat(), Options(topUp: true, rebuy: true), canStandUp: true, allowKick: true);

        states.Should().Be(new SeatMenu.ItemStates(TopUp: true, Rebuy: false, StandUp: true, Kick: true));
    }

    [TestMethod]
    public void GetItemStates_BetweenHandsWithABustedSeat_OffersRebuyInsteadOfTopUp()
    {
        var states = SeatMenu.GetItemStates(Seat(stack: 0m), Options(topUp: true, rebuy: true), canStandUp: true, allowKick: true);

        states.Should().Be(new SeatMenu.ItemStates(TopUp: false, Rebuy: true, StandUp: true, Kick: true));
    }

    [TestMethod]
    public void GetItemStates_BustedSeatWithChipsPending_TopsUpAgain()
    {
        var states = SeatMenu.GetItemStates(Seat(stack: 0m, pending: 50m), Options(topUp: true, rebuy: true), canStandUp: true, allowKick: true);

        states.Should().Be(new SeatMenu.ItemStates(TopUp: true, Rebuy: false, StandUp: true, Kick: true));
    }

    [TestMethod]
    public void GetItemStates_RebuyLimitReached_DisablesRebuy()
    {
        var states = SeatMenu.GetItemStates(Seat(stack: 0m, rebuys: 2), Options(rebuy: true, maxRebuys: 2), canStandUp: true, allowKick: true);

        states.Should().Be(new SeatMenu.ItemStates(TopUp: false, Rebuy: false, StandUp: true, Kick: true));
    }

    [TestMethod]
    public void GetItemStates_TopUpAndRebuyNotAllowed_DisablesBoth()
    {
        var withChips = SeatMenu.GetItemStates(Seat(), Options(), canStandUp: true, allowKick: true);
        var busted = SeatMenu.GetItemStates(Seat(stack: 0m), Options(), canStandUp: true, allowKick: true);

        withChips.Should().Be(new SeatMenu.ItemStates(false, false, true, true));
        busted.Should().Be(new SeatMenu.ItemStates(false, false, true, true));
    }

    [TestMethod]
    public void GetItemStates_NoOptionsYet_DisablesTopUpAndRebuy()
    {
        var states = SeatMenu.GetItemStates(Seat(), null, canStandUp: true, allowKick: true);

        states.Should().Be(new SeatMenu.ItemStates(false, false, true, true));
    }

    [TestMethod]
    public void GetItemStates_MidHand_OnlyKickIsAvailable()
    {
        var states = SeatMenu.GetItemStates(Seat(), Options(topUp: true, rebuy: true), canStandUp: false, allowKick: true);

        states.Should().Be(new SeatMenu.ItemStates(TopUp: false, Rebuy: false, StandUp: false, Kick: true));
    }

    [TestMethod]
    public void GetItemStates_SeatThatLeft_DisablesEverything()
    {
        var states = SeatMenu.GetItemStates(Seat(hasLeft: true), Options(topUp: true, rebuy: true), canStandUp: true, allowKick: true);

        states.Should().Be(default(SeatMenu.ItemStates));
    }

    [TestMethod]
    public void GetItemStates_TournamentTableBetweenHands_DisablesEverything()
    {
        var states = SeatMenu.GetItemStates(Seat(), Options(), canStandUp: false, allowKick: false);

        states.Should().Be(default(SeatMenu.ItemStates));
    }

    [TestMethod]
    public void GetItemStates_FreePositionBetweenHands_OffersOnlySitDown()
    {
        var placeholder = SeatMenu.GetItemStates(null, Options(topUp: true, rebuy: true), canStandUp: true, allowKick: true, canSitDown: true);
        var left = SeatMenu.GetItemStates(Seat(hasLeft: true), Options(topUp: true, rebuy: true), canStandUp: true, allowKick: true, canSitDown: true);

        placeholder.Should().Be(new SeatMenu.ItemStates(false, false, false, false, SitDown: true));
        left.Should().Be(placeholder);
    }

    [TestMethod]
    public void GetItemStates_FreePositionWithAQueuedPlayer_OffersOnlyCancelSitDown()
    {
        var states = SeatMenu.GetItemStates(null, Options(), canStandUp: true, allowKick: true, canSitDown: true, queued: true);

        states.Should().Be(new SeatMenu.ItemStates(false, false, false, false, CancelSitDown: true));
    }

    [TestMethod]
    public void GetItemStates_FreePositionWhereNoOneMaySit_DisablesEverything()
    {
        SeatMenu.GetItemStates(null, Options(), canStandUp: false, allowKick: false, canSitDown: false).Should().Be(default(SeatMenu.ItemStates));
    }

    [TestMethod]
    public void GetItemStates_OccupiedSeat_DisablesSitDown()
    {
        var states = SeatMenu.GetItemStates(Seat(), Options(), canStandUp: true, allowKick: true, canSitDown: true);

        states.SitDown.Should().BeFalse();
        states.CancelSitDown.Should().BeFalse();
    }

    private static IRenderedComponent<SeatMenu> RenderMenu(BunitContext ctx, List<byte> stoodUp, List<byte> kicked)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddAntDesign();
        return ctx.Render<SeatMenu>(parameters => parameters
            .Add(p => p.SeatNo, (byte)6)
            .Add(p => p.SeatData, Seat())
            .Add(p => p.CanStandUp, true)
            .Add(p => p.OnStandUp, stoodUp.Add)
            .Add(p => p.OnKick, kicked.Add));
    }

    /// <summary>
    /// Without the <c>AntContainer</c> of the demo's layout, which bUnit does not render, the dropdown and
    /// the popconfirm draw their overlays inside the component itself, so the test finds them there.
    /// </summary>
    private static void ChooseItem(IRenderedComponent<SeatMenu> menu, string label)
    {
        menu.Find(".seat-menu-trigger").Click();
        menu.WaitForAssertion(() => menu.FindAll(".seat-menu-item").Should().HaveCount(6));
        menu.FindAll(".seat-menu-item").Single(i => i.TextContent.Trim() == label).Click();
    }

    [TestMethod]
    public void Menu_ListsEveryItemInOrder_WithTheUnavailableOnesDisabled()
    {
        using var ctx = new BunitContext();
        var component = RenderMenu(ctx, [], []);
        component.Find(".seat-menu-trigger").Click();
        component.WaitForAssertion(() => component.FindAll(".seat-menu-item").Should().HaveCount(6));

        var items = component.FindAll(".seat-menu-item");

        items.Select(i => i.TextContent.Trim()).Should().Equal("Sit down", "Top up", "Rebuy", "Stand up", "Kick", "Cancel sit-down");
        items.Select(i => i.ClassList.Contains("ant-dropdown-menu-item-disabled")).Should().Equal(true, true, true, false, false, true);
        component.Find(".seat-menu-trigger").GetAttribute("aria-label").Should().Be("Actions for Player 6");
        component.Find(".seat-menu-trigger").GetAttribute("data-seat-no").Should().Be("6");
    }

    [TestMethod]
    [DataRow("Kick")]
    [DataRow("Stand up")]
    public void Menu_ConfirmingAnItem_RaisesTheSeatNumber(string label)
    {
        using var ctx = new BunitContext();
        var stoodUp = new List<byte>();
        var kicked = new List<byte>();
        var component = RenderMenu(ctx, stoodUp, kicked);

        ChooseItem(component, label);
        stoodUp.Should().BeEmpty("nothing happens before the confirmation");
        kicked.Should().BeEmpty("nothing happens before the confirmation");
        component.WaitForAssertion(() => component.Markup.Should().Contain($"{label} Player 6?"));
        component.FindAll(".ant-popover-buttons .ant-btn-primary").Single().Click();

        (label == "Kick" ? kicked : stoodUp).Should().Equal((byte)6);
        (label == "Kick" ? stoodUp : kicked).Should().BeEmpty();
    }

    [TestMethod]
    [DataRow("Kick")]
    [DataRow("Stand up")]
    public void Menu_CancellingAnItem_RaisesNothing(string label)
    {
        using var ctx = new BunitContext();
        var stoodUp = new List<byte>();
        var kicked = new List<byte>();
        var component = RenderMenu(ctx, stoodUp, kicked);

        ChooseItem(component, label);
        component.WaitForAssertion(() => component.FindAll(".ant-popover-buttons .ant-btn").Should().HaveCount(2));
        component.FindAll(".ant-popover-buttons .ant-btn:not(.ant-btn-primary)").Single().Click();

        stoodUp.Should().BeEmpty();
        kicked.Should().BeEmpty();
    }
}
