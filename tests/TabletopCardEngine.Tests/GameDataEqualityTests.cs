using FluentAssertions;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class GameDataEqualityTests
{
    private static readonly Guid _gameId = Guid.Parse("{16216216-2162-1621-6216-216216216216}");

    private static readonly GameOptionData _gameOptions = new() { MaxPlayersCount = 6, InitialSmallBlind = 5, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _seats =
    [
        new("Player 1", 1, 1000),
        new("Player 2", 2, 1000),
        new("Player 3", 3, 1000),
    ];

    [TestMethod]
    public async Task GameData_TwoGetGameCalls_AreEqualWithEqualHashCodes()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        await StartOnTheFlopAsync(engine);

        var first = (await engine.ApplyHoldemActionAsync(new HoldemActionGetGame(_gameId))).Value;
        var second = (await engine.ApplyHoldemActionAsync(new HoldemActionGetGame(_gameId))).Value;

        // Every collection the record holds has to be populated, or the test proves nothing about it.
        first.HandData!.Seats.Should().HaveCount(3);
        first.HandData.Seats.Should().OnlyContain(seat => seat.Hole != null && seat.Hole.Count == 2);
        first.HandData.CommunityCards.Should().HaveCount(3);
        first.HandData.Evaluations.Should().NotBeEmpty();

        ReferenceEquals(first, second).Should().BeFalse();
        (first == second).Should().BeTrue();
        first.Equals(second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [TestMethod]
    public async Task GameData_AfterAnAction_IsNotEqualToTheStateBefore()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var before = await StartOnTheFlopAsync(engine);

        var after = (await engine.ApplyHoldemActionAsync(new HoldemActionPlay(_gameId, PlayerActionType.Check, 0))).Value;

        (before == after).Should().BeFalse();
    }

    [TestMethod]
    public async Task HandData_Seats_CannotBeWrittenThroughACast()
    {
        using var host = TestEngineHost.Setup(out var engine, out _);
        var gameData = await StartOnTheFlopAsync(engine);
        var seats = gameData.HandData!.Seats;

        (seats as SeatData[]).Should().BeNull();
        var castToArray = () => (SeatData[])seats;
        castToArray.Should().Throw<InvalidCastException>();

        if (seats is IList<SeatData> list)
        {
            list.IsReadOnly.Should().BeTrue();
            var write = () => list[0] = list[1];
            write.Should().Throw<NotSupportedException>();
        }

        if (seats is ICollection<SeatData> collection)
        {
            collection.IsReadOnly.Should().BeTrue();
        }

        (gameData.HandData.CommunityCards as Card[]).Should().BeNull();
        (gameData.HandData.Evaluations as PlayerActionEvaluationData[]).Should().BeNull();
        (seats[0].Hole as Card[]).Should().BeNull();
    }

    private static async Task<GameData> StartOnTheFlopAsync(HoldemEngine engine)
    {
        (await engine.ApplyHoldemActionAsync(new HoldemActionCreateGame(_gameId, _gameOptions))).IsSuccess.Should().BeTrue();
        (await engine.ApplyHoldemActionAsync(new HoldemActionStartGame(_gameId, _seats))).IsSuccess.Should().BeTrue();
        (await engine.ApplyHoldemActionAsync(new HoldemActionPlay(_gameId, PlayerActionType.Call, 0))).IsSuccess.Should().BeTrue();
        (await engine.ApplyHoldemActionAsync(new HoldemActionPlay(_gameId, PlayerActionType.Call, 0))).IsSuccess.Should().BeTrue();
        var flop = await engine.ApplyHoldemActionAsync(new HoldemActionPlay(_gameId, PlayerActionType.Check, 0));
        flop.IsSuccess.Should().BeTrue();
        flop.Value.HandData!.Street.Should().Be(Street.Flop);
        return flop.Value;
    }
}
