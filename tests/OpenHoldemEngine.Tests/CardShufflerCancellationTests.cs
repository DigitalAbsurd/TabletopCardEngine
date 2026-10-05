using FluentAssertions;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Tests;

/// <summary>The caller's cancellation token has to reach the shuffler, or a shuffler doing I/O cannot honour it.</summary>
[TestClass]
public sealed class CardShufflerCancellationTests
{
    private static readonly Guid _gameId = Guid.Parse("{55555555-5555-5555-5555-555555555555}");

    [TestMethod]
    public async Task ApplyHoldemAction_ShufflerCalled_ReceivesCallersTokenForHoleAndBoardCards()
    {
        var shuffler = new RecordingShuffler();
        using var host = TestEngineHost.Setup(out var engine, out _, shuffler: shuffler);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        (await engine.ApplyHoldemActionAsync(
            new HoldemActionCreateGame(_gameId, new GameOptionData { MaxPlayersCount = 2, InitialSmallBlind = 5, InitialDealerSeatNo = 1 }), token))
            .IsSuccess.Should().BeTrue();
        (await engine.ApplyHoldemActionAsync(
            new HoldemActionStartGame(_gameId, [new("Alice", 1, 100), new("Bob", 2, 100)]), token))
            .IsSuccess.Should().BeTrue();

        shuffler.HoleTokens.Should().NotBeEmpty().And.OnlyContain(seen => seen == token);
        shuffler.DeckTokens.Should().BeEmpty();

        (await engine.ApplyHoldemActionAsync(new HoldemActionPlay(_gameId, PlayerActionType.Call, 0), token)).IsSuccess.Should().BeTrue();
        var flop = await engine.ApplyHoldemActionAsync(new HoldemActionPlay(_gameId, PlayerActionType.Check, 0), token);
        flop.IsSuccess.Should().BeTrue();
        flop.Value.HandData!.Street.Should().Be(Street.Flop);

        shuffler.DeckTokens.Should().NotBeEmpty().And.OnlyContain(seen => seen == token);
    }

    private sealed class RecordingShuffler : ICardShuffler
    {
        private static readonly Card[] _board = [.. new[] { "4s", "9h", "qd", "3c", "8s" }.Select(Card.Parse)];

        public List<CancellationToken> HoleTokens { get; } = [];

        public List<CancellationToken> DeckTokens { get; } = [];

        public ValueTask<IReadOnlyList<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo, CancellationToken cancellationToken)
        {
            HoleTokens.Add(cancellationToken);
            return ValueTask.FromResult<IReadOnlyList<Card>>(seatNo == 1
                ? [Card.Parse("ac"), Card.Parse("kc")]
                : [Card.Parse("7d"), Card.Parse("2h")]);
        }

        public ValueTask<IReadOnlyList<Card>> GetDeckCardsAsync(Guid gameId, int handNo, CancellationToken cancellationToken)
        {
            DeckTokens.Add(cancellationToken);
            return ValueTask.FromResult<IReadOnlyList<Card>>(_board);
        }
    }
}
