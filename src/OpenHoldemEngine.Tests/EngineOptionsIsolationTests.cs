using FluentAssertions;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Tests;

/// <summary>
/// Engine options belong to the container that registered them: two engines in one process, one
/// configured with a shuffler and one without, must each deal their own way.
/// </summary>
[TestClass]
public sealed class EngineOptionsIsolationTests
{
    private static readonly Guid _gameId = Guid.Parse("{44444444-4444-4444-4444-444444444444}");

    private static readonly GameOptionData _gameOptions = new(
        MaxPlayersCount: 2,
        InitialSmallBlind: 5,
        InitialDealerSeatNo: 1);

    private static readonly PlayerJoinData[] _joinSeats =
    [
        new("Alice", 1, 100),
        new("Bob", 2, 100),
    ];

    private static readonly Dictionary<byte, Card[]> _holeCards = new()
    {
        { 1, CardsFrom("ac", "kc") },
        { 2, CardsFrom("7d", "2h") },
    };

    private static readonly Card[] _board = CardsFrom("4s", "9h", "qd", "3c", "8s");

    [TestMethod]
    public async Task TwoContainers_OneWithShuffler_EachDealsItsOwnWay()
    {
        using var scriptedHost = TestEngineHost.Setup(out var scriptedEngine, out _,
            shuffler: new TestCardShuffler(getHoleFunc: (_, seatNo) => _holeCards[seatNo], getDeckFunc: _ => _board));
        using var randomHost = TestEngineHost.Setup(out var randomEngine, out _);

        // Both engines are alive at once, and each deals a hand before either plays on.
        var scriptedHand = await StartHandAsync(scriptedEngine);
        var randomHand = await StartHandAsync(randomEngine);

        HoleOf(scriptedHand, 1).Should().Equal(_holeCards[1]);
        HoleOf(scriptedHand, 2).Should().Equal(_holeCards[2]);

        // Four specific cards in a specific order: a random deal matches them about once in six
        // million, so a match means the shuffler leaked from the other container.
        HoleOf(randomHand, 1).Concat(HoleOf(randomHand, 2)).Should()
            .NotEqual(_holeCards[1].Concat(_holeCards[2]));

        // The shuffler is consulted again at the street change; the random engine dealing in
        // between must not have disturbed that either.
        var scriptedFlop = await CallAndCheckToFlopAsync(scriptedEngine);
        var randomFlop = await CallAndCheckToFlopAsync(randomEngine);

        scriptedFlop.Street.Should().Be(Street.Flop);
        scriptedFlop.CommunityCards.Should().Equal(_board.Take(3));
        randomFlop.Street.Should().Be(Street.Flop);
        randomFlop.CommunityCards.Should().HaveCount(3);
    }

    private static async Task<HandData> StartHandAsync(HoldemEngine engine)
    {
        (await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, _gameOptions)))
            .IsSuccess.Should().BeTrue();

        var started = await engine.ApplyHoldemActionAsync(new HoldemActionStartHand(_gameId) { JoinSeats = _joinSeats });
        started.IsSuccess.Should().BeTrue();
        return started.Value.HandData;
    }

    private static async Task<HandData> CallAndCheckToFlopAsync(HoldemEngine engine)
    {
        (await engine.ApplyHoldemActionAsync(new HoldemActionPlayerAction(_gameId, PlayerActionType.Call, 0)))
            .IsSuccess.Should().BeTrue();

        var checkedDown = await engine.ApplyHoldemActionAsync(new HoldemActionPlayerAction(_gameId, PlayerActionType.Check, 0));
        checkedDown.IsSuccess.Should().BeTrue();
        return checkedDown.Value.HandData;
    }

    private static Card[] HoleOf(HandData handData, byte seatNo)
        => handData.Seats.Single(seat => seat.SeatNo == seatNo).Hole!;

    private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.FromString)];
}
