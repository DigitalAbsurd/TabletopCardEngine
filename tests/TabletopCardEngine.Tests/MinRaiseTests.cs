using FluentAssertions;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The minimum raise is the standing wager plus the last full raise, never less than one big
/// blind: two big blinds pre-flop, and an incomplete all-in raise lifts the standing wager without
/// lifting the increment. Every case runs at blinds 1/2.
/// </summary>
[TestClass]
public sealed class MinRaiseTests
{
    private static readonly Guid _gameId = Guid.Parse("{44444444-4444-4444-4444-444444444444}");

    [TestMethod]
    public async Task MinRaise_UnderTheGun_IsTwoBigBlinds()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var data = await StartAsync(engine, dealer: 1, stacks: [1000, 1000, 1000]);

        data.HandData!.NextPlayerSeatNo.Should().Be(1);
        RaiseWindow(data).Should().Be((4M, 1000M));
    }

    [TestMethod]
    public async Task MinRaise_AfterARaiseToTen_IsEighteen()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await StartAsync(engine, dealer: 1, stacks: [1000, 1000, 1000]);

        var data = await PlayAsync(engine, PlayerActionType.Raise, 10);

        RaiseWindow(data).Min.Should().Be(18M);
    }

    [TestMethod]
    public async Task MinRaise_AfterAnIncompleteAllInRaise_IsTheStandingWagerPlusTheLastFullRaise()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        // Dealer is seat 4, so seat 1 posts the small blind, seat 2 the big blind and seat 3 is under the gun.
        await StartAsync(engine, dealer: 4, stacks: [13, 1000, 1000, 1000]);
        await PlayAsync(engine, PlayerActionType.Raise, 10);
        await PlayAsync(engine, PlayerActionType.Call);
        var data = await PlayAsync(engine, PlayerActionType.AllIn);

        data.HandData!.NextPlayerSeatNo.Should().Be(2);
        data.HandData.Seats.Single(seat => seat.SeatNo == 1).LastAction!.Amount.Should().Be(13M);
        RaiseWindow(data).Min.Should().Be(21M, "the all-in raised by 3, below the last full raise of 8");
    }

    [TestMethod]
    public async Task MinRaise_FirstBetOnTheFlop_IsOneBigBlind()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await StartAsync(engine, dealer: 1, stacks: [1000, 1000, 1000]);
        await PlayAsync(engine, PlayerActionType.Call);
        await PlayAsync(engine, PlayerActionType.Call);
        var data = await PlayAsync(engine, PlayerActionType.Check);

        data.HandData!.Street.Should().Be(Street.Flop);
        RaiseWindow(data).Min.Should().Be(2M);
    }

    [TestMethod]
    public async Task MinRaise_HeadsUpButtonPreFlop_IsTwoBigBlinds()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var data = await StartAsync(engine, dealer: 1, stacks: [1000, 1000]);

        RaiseWindow(data).Min.Should().Be(4M);
    }

    [TestMethod]
    public async Task MinRaise_ShortBigBlindPost_StillLeavesTwoBigBlinds()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);

        // Heads-up the button posts the small blind and acts first; seat 2 posts a big blind of 1.5 and is all in.
        var data = await StartAsync(engine, dealer: 1, stacks: [1000, 1.5M]);

        data.HandData!.Seats.Single(seat => seat.SeatNo == 2).LastAction!.Amount.Should().Be(1.5M);
        RaiseWindow(data).Min.Should().Be(4M);
    }

    private static (decimal Min, decimal Max) RaiseWindow(GameData data)
    {
        var raise = data.HandData!.Evaluations.Single(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);
        return (raise.MinValue, raise.MaxValue);
    }

    private static async Task<GameData> StartAsync(IHoldemTable engine, byte dealer, decimal[] stacks)
    {
        var options = new GameOptionData
        {
            MaxPlayersCount = (byte)stacks.Length,
            InitialSmallBlind = 1,
            InitialDealerSeatNo = dealer
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
