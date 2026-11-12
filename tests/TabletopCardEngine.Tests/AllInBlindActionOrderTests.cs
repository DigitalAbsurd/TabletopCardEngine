using FluentAssertions;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Who acts first pre-flop when a blind went all in on its post. A seat that is all in is not waiting,
/// so a first actor found by counting two waiting seats on from the small blind overshoots by one
/// seat. The ring here is six seats with the button on seat 1, so the small blind is seat 2, the big
/// blind seat 3 and under-the-gun seat 4, whatever the blinds' stacks are.
/// </summary>
[TestClass]
public sealed class AllInBlindActionOrderTests
{
    private const decimal SmallBlind = 50M;
    private const decimal Deep = 10_000M;
    private static readonly Card[] _holes = [.. new[] { "2c", "3c", "4c", "5c", "6c", "7c", "8c", "9c", "2d", "3d", "4d", "5d" }.Select(Card.Parse)];
    private static readonly Card[] _board = [.. new[] { "ad", "kd", "qd", "jd", "td" }.Select(Card.Parse)];
    private static readonly Guid _gameId = Guid.Parse("{44444444-4444-4444-4444-444444444444}");

    [TestMethod]
    [DataRow(BettingStructure.NoLimit)]
    [DataRow(BettingStructure.FixedLimit)]
    public async Task PreFlop_BigBlindStackEqualsTheBigBlind_UnderTheGunActsFirstAndTheBlindIsSkipped(BettingStructure structure)
    {
        var (first, order, street, next, _) = await PlayAsync(structure, [Deep, Deep, SmallBlind * 2, Deep, Deep, Deep], callers: 5);

        first.Should().Be(4);
        order.Should().Equal(4, 5, 6, 1, 2);
        street.Should().Be(Street.Flop);
        next.Should().Be(2);
    }

    [TestMethod]
    [DataRow(BettingStructure.NoLimit)]
    [DataRow(BettingStructure.FixedLimit)]
    public async Task PreFlop_SeatBelowTheBigBlindAtTheBigBlind_PostsWhatItHasAndIsAllIn(BettingStructure structure)
    {
        // The blinds go by position: seat 3 is the big blind whatever its stack, posts its 60 and is all in. The
        // standing wager is the 60 it posted, so the others call 60 and the small blind completes to it.
        var (first, order, street, next, last) = await PlayAsync(structure, [Deep, Deep, 60M, Deep, Deep, Deep], callers: 5);

        first.Should().Be(4);
        order.Should().Equal(4, 5, 6, 1, 2);
        street.Should().Be(Street.Flop);
        next.Should().Be(2);
        AssertPostedShort(last, seatNo: 3, HandActionType.BigBlind, posted: 60M, pot: 6 * 60M);
    }

    [TestMethod]
    [DataRow(BettingStructure.NoLimit)]
    [DataRow(BettingStructure.FixedLimit)]
    public async Task PreFlop_SeatBelowTheSmallBlindAtTheSmallBlind_PostsWhatItHasAndIsAllIn(BettingStructure structure)
    {
        var (first, order, street, next, last) = await PlayAsync(structure, [Deep, 30M, Deep, Deep, Deep, Deep], callers: 5);

        first.Should().Be(4);
        order.Should().Equal(4, 5, 6, 1, 3);
        street.Should().Be(Street.Flop);
        next.Should().Be(3);
        AssertPostedShort(last, seatNo: 2, HandActionType.SmallBlind, posted: 30M, pot: 30M + (5 * SmallBlind * 2));
    }

    [TestMethod]
    [DataRow(BettingStructure.NoLimit)]
    [DataRow(BettingStructure.FixedLimit)]
    public async Task PreFlop_SmallBlindAllInOnItsPost_UnderTheGunActsFirstAndTheBigBlindKeepsItsOption(BettingStructure structure)
    {
        var (first, order, street, next, _) = await PlayAsync(structure, [Deep, SmallBlind, Deep, Deep, Deep, Deep], callers: 5);

        first.Should().Be(4);
        order.Should().Equal(4, 5, 6, 1, 3);
        street.Should().Be(Street.Flop);
        next.Should().Be(3);
    }

    [TestMethod]
    [DataRow(BettingStructure.NoLimit)]
    [DataRow(BettingStructure.FixedLimit)]
    public async Task PreFlop_BothBlindsAllInOnTheirPosts_UnderTheGunActsFirstAndTheStreetClosesAfterTheButton(BettingStructure structure)
    {
        var (first, order, street, next, _) = await PlayAsync(structure, [Deep, SmallBlind, SmallBlind * 2, Deep, Deep, Deep], callers: 4);

        first.Should().Be(4);
        order.Should().Equal(4, 5, 6, 1);
        street.Should().Be(Street.Flop);
        next.Should().Be(4);
    }

    // A blind too short for its post is dealt in, posts its whole stack as the blind and takes no other action.
    private static void AssertPostedShort(HandData last, byte seatNo, HandActionType blind, decimal posted, decimal pot)
    {
        var seat = last.Seats.Single(seat => seat.SeatNo == seatNo);
        seat.Hole.Should().NotBeNull();
        seat.Stack.Should().Be(0M);
        last.Actions.Where(action => action.SeatNo == seatNo).Select(action => (action.ActionType, action.Amount))
            .Should().Equal((blind, posted));
        last.Pot.Should().Be(pot);
    }

    /// <summary>
    /// Deals a hand and has the given number of seats call (or check) in turn, recording who was on the clock
    /// before each call. Returns the first actor, the order, and the street and seat on the clock after
    /// the last call, with the last HandData.
    /// </summary>
    private static async Task<(byte First, List<byte> Order, Street Street, byte Next, HandData Last)> PlayAsync(
        BettingStructure structure, decimal[] stacks, int callers)
    {
        var options = new GameOptionData
        {
            MaxPlayersCount = 6,
            InitialSmallBlind = SmallBlind,
            InitialDealerSeatNo = 1,
            BettingStructure = structure
        };
        using var host = TestEngineHost.Setup(out var engine, out _, shuffler: new TestCardShuffler(
            getHoleFunc: (_, seatNo) => [_holes[(seatNo - 1) * 2], _holes[((seatNo - 1) * 2) + 1]],
            getDeckFunc: _ => _board));

        (await engine.ApplyAsync(new HoldemActionCreateGame(_gameId, options))).IsSuccess.Should().BeTrue();
        var seats = stacks.Select((stack, index) => new PlayerJoinData($"P{index + 1}", (byte)(index + 1), stack)).ToArray();
        var started = await engine.ApplyAsync(new HoldemActionStartGame(_gameId, seats));
        started.IsSuccess.Should().BeTrue(string.Join("; ", started.Errors.Select(error => error.Message)));

        var handData = started.Value.HandData!;
        var first = handData.NextPlayerSeatNo;
        var order = new List<byte>();
        for (var i = 0; i < callers; i++)
        {
            order.Add(handData.NextPlayerSeatNo);

            // The big blind's option has nothing to call, so it checks.
            var action = handData.Evaluations.Any(evaluation => evaluation.PlayerActionType == PlayerActionType.Call)
                ? PlayerActionType.Call
                : PlayerActionType.Check;
            var result = await engine.ApplyAsync(new HoldemActionPlay(_gameId, action, 0));
            result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
            handData = result.Value.HandData!;
        }

        return (first, order, handData.Street, handData.NextPlayerSeatNo, handData);
    }
}
