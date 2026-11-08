using FluentAssertions;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The hand-built Pot-Limit suite. The largest raise is the standing wager plus the pot after the seat
/// calls, blinds and folded seats' wagers included, and then no more than the stack. The first two
/// windows are pokerkit 0.7.7's (blinds 1/2, three seats of 200); every case runs at blinds 1/2.
/// </summary>
[TestClass]
public sealed class PotLimitTests
{
    private static readonly Guid _gameId = Guid.Parse("{65265265-2652-6526-5265-265265265265}");

    [TestMethod]
    public async Task PotLimit_PreFlop_MatchesThePokerkitWindows()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        // Dealer is seat 3, so seat 1 posts the small blind, seat 2 the big blind and seat 3 acts first.
        var data = await StartAsync(engine, dealer: 3, stacks: [200, 200, 200]);
        data.HandData!.NextPlayerSeatNo.Should().Be(3);
        RaiseWindow(data).Should().Be((4M, 7M), "2 + (3 + 2): the big blind plus the pot after the call");
        await AssertIllegalAsync(engine, data, PlayerActionType.Raise, 8);

        data = await PlayAsync(engine, PlayerActionType.Raise, 7);

        data.HandData!.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((12M, 23M), "7 + (10 + 6): the raise plus the pot after the small blind calls");
    }

    [TestMethod]
    public async Task PotLimit_FirstBetOnTheFlop_IsCappedAtThePot()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var data = await PotOfThirtyOnTheFlopAsync(engine);

        data.HandData!.NextPlayerSeatNo.Should().Be(2);
        data.HandData.Evaluations.Should().Contain(evaluation => evaluation.PlayerActionType == PlayerActionType.Check);
        RaiseWindow(data).Should().Be((2M, 30M));
    }

    [TestMethod]
    public async Task PotLimit_RaiseAfterABetOfTenIntoThirty_IsCappedAtSixty()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await PotOfThirtyOnTheFlopAsync(engine);

        var data = await PlayAsync(engine, PlayerActionType.Bet, 10);

        data.HandData!.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((20M, 60M), "10 + (40 + 10): the bet plus the pot after the call");
    }

    [TestMethod]
    public async Task PotLimit_FoldedSeat_ItsWagerStillCountsInThePot()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await StartAsync(engine, dealer: 3, stacks: [200, 200, 200]);
        await PlayAsync(engine, PlayerActionType.Raise, 7);

        var data = await PlayAsync(engine, PlayerActionType.Fold);

        data.HandData!.NextPlayerSeatNo.Should().Be(2);
        RaiseWindow(data).Should().Be((12M, 22M), "7 + (10 + 5): the folded small blind's 1 is still in the pot");
    }

    [TestMethod]
    public async Task PotLimit_ShortStack_TheMaximumIsTheStack()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        var data = await StartAsync(engine, dealer: 3, stacks: [200, 200, 5]);

        RaiseWindow(data).Should().Be((4M, 5M), "the pot allows 7 but the seat has only 5");
    }

    [TestMethod]
    public async Task PotLimit_StackBelowTheMinimum_IsOfferedOnlyTheAllInRaise()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        var data = await StartAsync(engine, dealer: 3, stacks: [200, 200, 3]);

        RaiseWindow(data).Should().Be((3M, 3M), "an incomplete all-in raise is the only raise the stack can make");
    }

    [TestMethod]
    public async Task PotLimit_AllInAboveThePot_IsIllegal()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        var data = await StartAsync(engine, dealer: 3, stacks: [200, 200, 200]);

        await AssertIllegalAsync(engine, data, PlayerActionType.AllIn);
    }

    [TestMethod]
    public async Task PotLimit_AllInBelowThePot_IsARaiseToTheStack()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await StartAsync(engine, dealer: 3, stacks: [200, 200, 6]);

        var data = await PlayAsync(engine, PlayerActionType.AllIn);

        var allIn = data.HandData!.Seats.Single(seat => seat.SeatNo == 3).LastAction!;
        allIn.ActionType.Should().Be(HandActionType.AllIn);
        allIn.Amount.Should().Be(6M);
        data.HandData.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((10M, 20M), "6 + (9 + 5): the all-in to 6 is a full raise of 4");
    }

    [TestMethod]
    public async Task PotLimit_HeadsUpButton_IsCappedAtSix()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        // Heads-up the button posts the small blind and acts first.
        var data = await StartAsync(engine, dealer: 1, stacks: [200, 200]);

        data.HandData!.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((4M, 6M), "2 + (3 + 1): the big blind plus the pot after the button calls");
    }

    [TestMethod]
    public async Task PotLimit_AfterABlindLevelChange_TheLimitFollowsTheHandsBlinds()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var schedule = new BlindScheduleData { IncreaseEveryHands = 1, Multiplier = 2M };
        await StartAsync(engine, dealer: 3, stacks: [1000, 1000, 1000], schedule);
        await PlayAsync(engine, PlayerActionType.Fold);
        await PlayAsync(engine, PlayerActionType.Fold);

        // Hand 2 is dealt at blinds 2/4; the button moves to seat 1, so seat 1 acts first.
        var data = await ApplyAsync(engine, new HoldemActionStartHand(_gameId));

        data.HandData!.SmallBlind.Should().Be(2M);
        data.HandData.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((8M, 14M), "4 + (6 + 4): the hand's big blind plus the pot after the call");
    }

    // Heads-up the button raises to 5, the big blind re-raises the pot to 15 (5 + (7 + 3)) and the button
    // calls, so the flop opens with 30 in the pot and the big blind, seat 2, to act.
    private static async Task<GameData> PotOfThirtyOnTheFlopAsync(IHoldemTable engine)
    {
        await StartAsync(engine, dealer: 1, stacks: [200, 200]);
        await PlayAsync(engine, PlayerActionType.Raise, 5);
        await PlayAsync(engine, PlayerActionType.Raise, 15);
        var data = await PlayAsync(engine, PlayerActionType.Call);
        data.HandData!.Street.Should().Be(Street.Flop);
        data.HandData.Pot.Should().Be(30M);
        return data;
    }

    private static async Task AssertIllegalAsync(IHoldemTable engine, GameData before, PlayerActionType action, decimal size = 0)
    {
        var result = await engine.ApplyAsync(new HoldemActionPlay(_gameId, action, size));

        result.IsFailed.Should().BeTrue($"{action} {size} should be rejected");
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(HoldemErrorCode.IllegalAction);

        var after = await ApplyAsync(engine, new HoldemActionGetGame(_gameId));
        after.Version.Should().Be(before.Version);
        after.HandData!.Actions.Should().HaveCount(before.HandData!.Actions.Count);
    }

    private static (decimal Min, decimal Max) RaiseWindow(GameData data)
    {
        var raise = data.HandData!.Evaluations.Single(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);
        return (raise.MinValue, raise.MaxValue);
    }

    private static async Task<GameData> StartAsync(IHoldemTable engine, byte dealer, decimal[] stacks, BlindScheduleData? schedule = null)
    {
        var options = new GameOptionData
        {
            BettingStructure = BettingStructure.PotLimit,
            MaxPlayersCount = (byte)stacks.Length,
            InitialSmallBlind = 1,
            InitialDealerSeatNo = dealer,
            BlindSchedule = schedule
        };
        await ApplyAsync(engine, new HoldemActionCreateGame(_gameId, options));
        var seats = stacks.Select((stack, index) => new PlayerJoinData($"P{index + 1}", (byte)(index + 1), stack)).ToArray();
        return await ApplyAsync(engine, new HoldemActionStartGame(_gameId, seats));
    }

    private static Task<GameData> PlayAsync(IHoldemTable engine, PlayerActionType action, decimal size = 0)
        => ApplyAsync(engine, new HoldemActionPlay(_gameId, action, size));

    private static async Task<GameData> ApplyAsync(IHoldemTable engine, HoldemAction action)
    {
        var result = await engine.ApplyAsync(action);
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
        return result.Value;
    }
}
