using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Demo.Phh;

/// <summary>
/// The cards a hand history dictates, arranged the way <see cref="ICardShuffler"/> wants them:
/// hole cards per seat and the board as one ordered run.
///
/// This is a port of <c>TabletopCardEngine.Tests.Phh.PhhHandDeck</c> into the demo project.
/// See the related test project for the original definition and the possible future consolidation
/// into a shared <c>TabletopCardEngine.Phh</c> project (see docs/task-run.md).
///
/// A history need not name every card — hole cards can be <c>??</c>, and a hand that ends before the
/// river simply has no board left to record. Those gaps are filled from the unused part of the deck
/// in a fixed order rather than at random, so a failing hand always fails the same way. Filler never
/// decides a showdown: <c>PhhSupport.IsSupported</c> rejects a hand that reaches one without
/// every contender's cards written down.
///
/// Hole cards are read from the deal entries and from the show/muck entries alike. Corpora of bot
/// play record every hand dealt (<c>d dh p1 3c9s</c>); corpora of real play only record what was
/// shown at the end (<c>p1 sm 3c9s</c>), and both name the same two cards.
/// </summary>
public sealed record PhhHandDeck
{
    private const int BoardCardsCount = 5;
    private const int HoleCardsPerSeat = 2;

    /// <summary>Keyed by one-based seat number, two cards each.</summary>
    public required IReadOnlyDictionary<byte, Card[]> HoleCards { get; init; }

    /// <summary>Five board cards: everything the history recorded, then filler.</summary>
    public required Card[] BoardCards { get; init; }

    /// <summary>Only the board cards the history actually recorded.</summary>
    public required Card[] RecordedBoardCards { get; init; }

    public static PhhHandDeck Create(PhhHand hand)
    {
        var recordedHoleCards = Enumerable.Range(0, hand.PlayerCount)
            .Select(playerIx => (SeatNo: (byte)(playerIx + 1), Cards: RecordedHoleCards(hand, playerIx)))
            .Where(entry => entry.Cards.Length > 0)
            .ToDictionary(entry => entry.SeatNo, entry => entry.Cards);
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
            RecordedBoardCards = [.. boardCards.Take(recordedBoard.Length)]
        };
    }

    /// <summary>
    /// What the history says one player held, merging the deal entry with the show/muck entry — a
    /// card is unknown only when neither names it. Empty when the history says nothing at all.
    /// </summary>
    public static Card?[] RecordedHoleCards(PhhHand hand, int playerIndex)
    {
        var dealt = hand.Actions.OfType<PhhDealHoleAction>()
            .LastOrDefault(deal => deal.PlayerIndex == playerIndex)?.Cards ?? [];
        var shown = hand.Actions.OfType<PhhShowMuckAction>()
            .LastOrDefault(show => show.PlayerIndex == playerIndex)?.Cards ?? [];

        return [.. Enumerable
            .Range(0, Math.Max(dealt.Length, shown.Length))
            .Select(cardIx => At(dealt, cardIx) ?? At(shown, cardIx))];

        static Card? At(Card?[] cards, int cardIx) => cardIx < cards.Length ? cards[cardIx] : null;
    }
}
