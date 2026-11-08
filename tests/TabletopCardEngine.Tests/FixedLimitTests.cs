using FluentAssertions;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The hand-built Fixed-Limit suite. Every bet and raise is one bet over the standing wager: the hand's
/// big blind pre-flop and on the flop, twice that on the turn and river. A street holds at most
/// <see cref="GameOptionData.RaiseCap"/> bets, the big blind counting as the first pre-flop, and an all-in
/// raise of at least half a bet counts as a full one: it reopens the betting and counts toward the cap.
/// Every case runs at blinds 1/2 unless a blind schedule raises them.
/// </summary>
[TestClass]
public sealed class FixedLimitTests
{
    private static readonly Guid _gameId = Guid.Parse("{65365365-3653-6536-5365-365365365365}");

    [TestMethod]
    public async Task FixedLimit_BetSizes_SmallBetThenBigBet()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        // Dealer is seat 3, so seat 1 posts the small blind, seat 2 the big blind and seat 3 acts first.
        var data = await StartAsync(engine, dealer: 3, stacks: [200, 200, 200]);
        data.HandData!.NextPlayerSeatNo.Should().Be(3);
        RaiseWindow(data).Should().Be((4M, 4M), "the big blind of 2 plus one small bet");
        await PlayAsync(engine, PlayerActionType.Raise, 4);
        await PlayAsync(engine, PlayerActionType.Call);
        data = await PlayAsync(engine, PlayerActionType.Call);

        data.HandData!.Street.Should().Be(Street.Flop);
        data.HandData.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((2M, 2M), "the flop is still bet in small bets");
        await PlayAsync(engine, PlayerActionType.Bet, 2);
        await PlayAsync(engine, PlayerActionType.Call);
        data = await PlayAsync(engine, PlayerActionType.Call);

        data.HandData!.Street.Should().Be(Street.Turn);
        RaiseWindow(data).Should().Be((4M, 4M), "the turn is bet in big bets");
        await PlayAsync(engine, PlayerActionType.Bet, 4);
        await PlayAsync(engine, PlayerActionType.Call);
        data = await PlayAsync(engine, PlayerActionType.Call);

        data.HandData!.Street.Should().Be(Street.River);
        RaiseWindow(data).Should().Be((4M, 4M));
        data = await PlayAsync(engine, PlayerActionType.Bet, 4);

        data.HandData!.NextPlayerSeatNo.Should().Be(2);
        RaiseWindow(data).Should().Be((8M, 8M), "a raise over a bet of 4 on the river goes to 8");
        data = await PlayAsync(engine, PlayerActionType.Raise, 8);
        data.HandData!.Seats.Single(seat => seat.SeatNo == 2).LastAction!.Amount.Should().Be(8M);
    }

    [TestMethod]
    public async Task FixedLimit_DefaultCap_FourthBetIsOfferedAndAFifthIsNot()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await StartAsync(engine, dealer: 3, stacks: [200, 200, 200]);
        await PlayAsync(engine, PlayerActionType.Raise, 4);
        var data = await PlayAsync(engine, PlayerActionType.Raise, 6);

        data.HandData!.NextPlayerSeatNo.Should().Be(2);
        RaiseWindow(data).Should().Be((8M, 8M), "the big blind, 4 and 6 are three bets, so a fourth is still allowed");
        data = await PlayAsync(engine, PlayerActionType.Raise, 8);

        data.HandData!.NextPlayerSeatNo.Should().Be(3);
        data.HandData.Evaluations.ShouldAsJToken().BeEquivalentToAsJToken(new[]
        {
            new PlayerActionEvaluationData(PlayerActionType.Fold),
            new PlayerActionEvaluationData(PlayerActionType.Call, 4M, 4M)
        });
        await AssertIllegalAsync(engine, data, PlayerActionType.Raise, 10);
        await AssertIllegalAsync(engine, data, PlayerActionType.AllIn);
    }

    [TestMethod]
    public async Task FixedLimit_NoCap_OffersAFifthBet()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await StartAsync(engine, dealer: 3, stacks: [200, 200, 200], raiseCap: null);
        await PlayAsync(engine, PlayerActionType.Raise, 4);
        await PlayAsync(engine, PlayerActionType.Raise, 6);
        var data = await PlayAsync(engine, PlayerActionType.Raise, 8);

        data.HandData!.NextPlayerSeatNo.Should().Be(3);
        RaiseWindow(data).Should().Be((10M, 10M));
        data = await PlayAsync(engine, PlayerActionType.Raise, 10);
        RaiseWindow(data).Should().Be((12M, 12M));
    }

    [TestMethod]
    public async Task FixedLimit_CapOfTwo_StopsAfterOneRaise()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var data = await StartAsync(engine, dealer: 3, stacks: [200, 200, 200], raiseCap: 2);
        RaiseWindow(data).Should().Be((4M, 4M));

        data = await PlayAsync(engine, PlayerActionType.Raise, 4);

        data.HandData!.NextPlayerSeatNo.Should().Be(1);
        data.HandData.Evaluations.Should().NotContain(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise,
            "the big blind and the raise to 4 are the two bets the cap allows");
        await AssertIllegalAsync(engine, data, PlayerActionType.Raise, 6);
        await AssertIllegalAsync(engine, data, PlayerActionType.AllIn);
    }

    [TestMethod]
    public async Task FixedLimit_AllInForLessThanHalfABet_DoesNotReopenOrCount()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        // Seat 2 reaches the turn with 5 behind and goes all in over seat 1's bet of 4: a rise of 1.
        var data = await BetFourOnTheTurnThenAllInAsync(engine, allInTo: 5, raiseCap: 2);

        data.HandData!.NextPlayerSeatNo.Should().Be(3);
        RaiseWindow(data).Should().Be((9M, 9M), "the all-in to 5 plus one big bet; with a cap of 2 it did not count");
        data = await PlayAsync(engine, PlayerActionType.Call);

        data.HandData!.NextPlayerSeatNo.Should().Be(1);
        data.HandData.Evaluations.ShouldAsJToken().BeEquivalentToAsJToken(new[]
        {
            new PlayerActionEvaluationData(PlayerActionType.Fold),
            new PlayerActionEvaluationData(PlayerActionType.Call, 1M, 1M)
        });
        await AssertIllegalAsync(engine, data, PlayerActionType.Raise, 9);
        await AssertIllegalAsync(engine, data, PlayerActionType.AllIn);
    }

    [TestMethod]
    public async Task FixedLimit_AllInForExactlyHalfABet_ReopensTheBetting()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var data = await BetFourOnTheTurnThenAllInAsync(engine, allInTo: 6, raiseCap: 4);

        data.HandData!.NextPlayerSeatNo.Should().Be(3);
        RaiseWindow(data).Should().Be((10M, 10M), "the all-in to 6 plus one big bet");
        data = await PlayAsync(engine, PlayerActionType.Call);

        data.HandData!.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((10M, 10M), "a rise of half a bet reopens the betting for the bettor");
    }

    [TestMethod]
    public async Task FixedLimit_AllInForExactlyHalfABet_CountsTowardTheCap()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var data = await BetFourOnTheTurnThenAllInAsync(engine, allInTo: 6, raiseCap: 2);

        data.HandData!.NextPlayerSeatNo.Should().Be(3);
        data.HandData.Evaluations.Should().NotContain(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise,
            "the bet of 4 and the all-in to 6 are the two bets the cap allows");
        await AssertIllegalAsync(engine, data, PlayerActionType.AllIn);
    }

    [TestMethod]
    public async Task FixedLimit_HeadsUp_TheCapStillApplies()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        // Heads-up the button posts the small blind and acts first.
        var data = await StartAsync(engine, dealer: 1, stacks: [200, 200]);
        data.HandData!.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((4M, 4M));
        await PlayAsync(engine, PlayerActionType.Raise, 4);
        await PlayAsync(engine, PlayerActionType.Raise, 6);
        data = await PlayAsync(engine, PlayerActionType.Raise, 8);

        data.HandData!.NextPlayerSeatNo.Should().Be(2);
        data.HandData.Evaluations.Should().NotContain(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);
        await AssertIllegalAsync(engine, data, PlayerActionType.Raise, 10);
    }

    [TestMethod]
    public async Task FixedLimit_AfterABlindLevelChange_EveryBetSizeDoubles()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var schedule = new BlindScheduleData { IncreaseEveryHands = 1, Multiplier = 2M };
        await StartAsync(engine, dealer: 3, stacks: [1000, 1000, 1000], schedule: schedule);
        await PlayAsync(engine, PlayerActionType.Fold);
        await PlayAsync(engine, PlayerActionType.Fold);

        // Hand 2 is dealt at blinds 2/4; the button moves to seat 1, so seat 1 acts first.
        var data = await ApplyAsync(engine, new HoldemActionStartHand(_gameId));

        data.HandData!.SmallBlind.Should().Be(2M);
        data.HandData.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((8M, 8M), "the hand's big blind of 4 plus one small bet of 4");
        await PlayAsync(engine, PlayerActionType.Call);
        await PlayAsync(engine, PlayerActionType.Call);
        data = await PlayAsync(engine, PlayerActionType.Check);

        data.HandData!.Street.Should().Be(Street.Flop);
        RaiseWindow(data).Should().Be((4M, 4M));
        await PlayAsync(engine, PlayerActionType.Check);
        await PlayAsync(engine, PlayerActionType.Check);
        data = await PlayAsync(engine, PlayerActionType.Check);

        data.HandData!.Street.Should().Be(Street.Turn);
        RaiseWindow(data).Should().Be((8M, 8M));
    }

    [TestMethod]
    public async Task FixedLimit_ShortBigBlind_DoesNotChangeTheBetSize()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        // Heads-up the big blind posts what it has; the button posts the small blind and is asked first.
        var data = await StartAsync(engine, dealer: 1, stacks: [200, 1.5M]);

        data.HandData!.BigBlindSeatNo.Should().Be(2);
        data.HandData.Seats.Single(seat => seat.SeatNo == 2).LastAction!.Amount.Should().Be(1.5M);
        data.HandData.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((4M, 4M), "the bet is one big blind of 2 whatever was posted");
    }

    [TestMethod]
    [DataRow(3)]
    [DataRow(5)]
    public async Task FixedLimit_RaiseOfTheWrongSize_IsIllegal(int size)
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        var data = await StartAsync(engine, dealer: 3, stacks: [200, 200, 200]);

        await AssertIllegalAsync(engine, data, PlayerActionType.Raise, size);
    }

    [TestMethod]
    public async Task FixedLimit_AllInAboveTheRaiseToTotal_IsIllegal()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        var data = await StartAsync(engine, dealer: 3, stacks: [200, 200, 5]);

        await AssertIllegalAsync(engine, data, PlayerActionType.AllIn);
    }

    [TestMethod]
    public async Task FixedLimit_StackBelowTheRaiseToTotal_IsOfferedTheAllInRaise()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var data = await StartAsync(engine, dealer: 3, stacks: [200, 200, 3]);

        RaiseWindow(data).Should().Be((3M, 3M), "the stack covers the call of 2 but not the raise to 4");
        data = await PlayAsync(engine, PlayerActionType.AllIn);

        var allIn = data.HandData!.Seats.Single(seat => seat.SeatNo == 3).LastAction!;
        allIn.ActionType.Should().Be(HandActionType.AllIn);
        allIn.Amount.Should().Be(3M);
        RaiseWindow(data).Should().Be((5M, 5M), "the all-in to 3 plus one small bet");
    }

    // Seat 3 and seat 1 call pre-flop, seat 2 checks, and the flop is checked through. On the turn seat 1
    // bets 4 and seat 2 goes all in with what it has left, which puts seat 3 on the clock.
    private static async Task<GameData> BetFourOnTheTurnThenAllInAsync(IHoldemTable engine, decimal allInTo, int raiseCap)
    {
        await StartAsync(engine, dealer: 3, stacks: [200, 2 + allInTo, 200], raiseCap: raiseCap);
        await PlayAsync(engine, PlayerActionType.Call);
        await PlayAsync(engine, PlayerActionType.Call);
        await PlayAsync(engine, PlayerActionType.Check);
        await PlayAsync(engine, PlayerActionType.Check);
        await PlayAsync(engine, PlayerActionType.Check);
        var data = await PlayAsync(engine, PlayerActionType.Check);
        data.HandData!.Street.Should().Be(Street.Turn);
        data.HandData.NextPlayerSeatNo.Should().Be(1);

        await PlayAsync(engine, PlayerActionType.Bet, 4);
        data = await PlayAsync(engine, PlayerActionType.AllIn);
        data.HandData!.Seats.Single(seat => seat.SeatNo == 2).LastAction!.Amount.Should().Be(allInTo);
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

    private static async Task<GameData> StartAsync(
        IHoldemTable engine, byte dealer, decimal[] stacks, int? raiseCap = 4, BlindScheduleData? schedule = null)
    {
        var options = new GameOptionData
        {
            BettingStructure = BettingStructure.FixedLimit,
            MaxPlayersCount = (byte)stacks.Length,
            InitialSmallBlind = 1,
            InitialDealerSeatNo = dealer,
            BlindSchedule = schedule,
            RaiseCap = raiseCap
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
