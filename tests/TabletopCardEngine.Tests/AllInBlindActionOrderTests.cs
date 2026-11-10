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
        var (first, order, street, next) = await PlayAsync(structure, [Deep, Deep, SmallBlind * 2, Deep, Deep, Deep], callers: 5);

        first.Should().Be(4);
        order.Should().Equal(4, 5, 6, 1, 2);
        street.Should().Be(Street.Flop);
        next.Should().Be(2);
    }

    [TestMethod]
    [DataRow(BettingStructure.NoLimit)]
    [DataRow(BettingStructure.FixedLimit)]
    public async Task PreFlop_SeatBelowTheBigBlindAtTheBigBlind_IsSatOutAndTheBlindPassesOn(BettingStructure structure)
    {
        // With three or more funded seats a stack that cannot cover the blind is sat out rather than
        // posted short (only heads-up does that, see HeadsUpTests), so seat 4 is the big blind here.
        var (first, order, _, _) = await PlayAsync(structure, [Deep, Deep, 60M, Deep, Deep, Deep], callers: 4);

        first.Should().Be(5);
        order.Should().Equal(5, 6, 1, 2);
    }

    [TestMethod]
    [DataRow(BettingStructure.NoLimit)]
    [DataRow(BettingStructure.FixedLimit)]
    public async Task PreFlop_SmallBlindAllInOnItsPost_UnderTheGunActsFirstAndTheBigBlindKeepsItsOption(BettingStructure structure)
    {
        var (first, order, street, next) = await PlayAsync(structure, [Deep, SmallBlind, Deep, Deep, Deep, Deep], callers: 5);

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
        var (first, order, street, next) = await PlayAsync(structure, [Deep, SmallBlind, SmallBlind * 2, Deep, Deep, Deep], callers: 4);

        first.Should().Be(4);
        order.Should().Equal(4, 5, 6, 1);
        street.Should().Be(Street.Flop);
        next.Should().Be(4);
    }

    /// <summary>
    /// Deals a hand and has the given number of seats call (or check) in turn, recording who was on the clock
    /// before each call. Returns the first actor, the order, and the street and seat on the clock after
    /// the last call.
    /// </summary>
    private static async Task<(byte First, List<byte> Order, Street Street, byte Next)> PlayAsync(
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

        return (first, order, handData.Street, handData.NextPlayerSeatNo);
    }
}
