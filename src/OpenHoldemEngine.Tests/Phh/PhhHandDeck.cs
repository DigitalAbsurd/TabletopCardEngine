using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Tests.Phh;

/// <summary>
/// The cards a hand history dictates, arranged the way <see cref="ICardShuffler"/> wants them:
/// hole cards per seat and the board as one ordered run.
/// </summary>
/// <remarks>
/// A history need not name every card — hole cards can be <c>??</c>, and a hand that ends before the
/// river simply has no board left to record. Those gaps are filled from the unused part of the deck
/// in a fixed order rather than at random, so a failing hand always fails the same way. Filler never
/// reaches a showdown: the engine only needs the full board when the history recorded one.
/// </remarks>
internal sealed record PhhHandDeck
{
    private const int BoardCardsCount = 5;
    private const int HoleCardsPerSeat = 2;

    /// <summary>Keyed by one-based seat number, two cards each.</summary>
    internal required IReadOnlyDictionary<byte, Card[]> HoleCards { get; init; }

    /// <summary>Five board cards: everything the history recorded, then filler.</summary>
    internal required Card[] BoardCards { get; init; }

    /// <summary>Only the board cards the history actually recorded.</summary>
    internal required Card[] RecordedBoardCards { get; init; }

    /// <summary>
    /// The prefix of <see cref="RecordedBoardCards"/> that was dealt while betting was still open.
    /// See <see cref="PhhHand.BoardDealtDuringBetting"/>.
    /// </summary>
    internal required Card[] BoardCardsDuringBetting { get; init; }

    internal static PhhHandDeck Create(PhhHand hand)
    {
        var recordedHoleCards = hand.Actions.OfType<PhhDealHoleAction>()
            .ToDictionary(deal => (byte)(deal.PlayerIndex + 1), deal => deal.Cards);
        var recordedBoard = hand.Board;

        var unusedCards = new Queue<Card>(PhhCards.FullDeck
            .Except(recordedHoleCards.Values.SelectMany(cards => cards).Concat(recordedBoard).OfType<Card>()));

        var holeCards = Enumerable.Range(1, hand.PlayerCount)
            .Select(seatNo => (byte)seatNo)
            .ToDictionary(
                seatNo => seatNo,
                seatNo => Enumerable
                    .Range(0, HoleCardsPerSeat)
                    .Select(cardIx => recordedHoleCards.TryGetValue(seatNo, out var cards) && cardIx < cards.Length
                        ? cards[cardIx] ?? unusedCards.Dequeue()
                        : unusedCards.Dequeue())
                    .ToArray());

        var boardCards = recordedBoard
            .Select(card => card ?? unusedCards.Dequeue())
            .Concat(Enumerable.Range(0, Math.Max(0, BoardCardsCount - recordedBoard.Length)).Select(_ => unusedCards.Dequeue()))
            .ToArray();

        return new PhhHandDeck
        {
            HoleCards = holeCards,
            BoardCards = boardCards,
            RecordedBoardCards = [.. boardCards.Take(recordedBoard.Length)],
            BoardCardsDuringBetting = [.. boardCards.Take(hand.BoardDealtDuringBetting.Length)]
        };
    }
}
