using System.Security.Cryptography;

namespace OpenHoldemEngine.Engine;

/// <summary>
/// The remaining, undealt cards for one hand — either a real shuffle or a fixed sequence an
/// <see cref="ICardShuffler"/> has already decided on. Replaces NPokerEngine's <c>Deck</c>: no
/// card-id encoding, no cheat-flag/reverse dance (see <see cref="Predetermined"/>), no
/// serialization, no cloning.
/// </summary>
internal sealed class CardDeck
{
    private readonly Queue<Card> _cards;

    private CardDeck(IEnumerable<Card> cards) => _cards = new Queue<Card>(cards);

    /// <summary>
    /// A deck that deals exactly these cards, in this order. NPokerEngine's cheat deck stores its
    /// cards reversed because it draws from the tail of a list; a plain queue in the given order
    /// has the same observable effect with none of the indirection.
    /// </summary>
    internal static CardDeck Predetermined(IEnumerable<Card> cards) => new(cards);

    /// <summary>
    /// A freshly shuffled deck, minus any cards already known to be dealt (hole cards, board
    /// cards). Excluding them is what keeps a random deal from ever repeating a card that is
    /// already on the table or in someone's hand.
    /// </summary>
    internal static CardDeck Shuffled(IEnumerable<Card>? exclude = null)
    {
        var excluded = exclude is null ? new HashSet<Card>() : [.. exclude];
        var cards = FullDeck().Where(card => !excluded.Contains(card)).ToList();
        Shuffle(cards);
        return new CardDeck(cards);
    }

    internal Card Draw()
        => _cards.Count > 0 ? _cards.Dequeue() : throw new InvalidOperationException("The deck is empty.");

    internal List<Card> Draw(int count) => [.. Enumerable.Range(0, count).Select(_ => Draw())];

    private static IEnumerable<Card> FullDeck()
        => Enum.GetValues<CardSuit>()
            .SelectMany(suit => Enum.GetValues<CardType>().Select(type => new Card { CardSuit = suit, CardType = type }));

    private static void Shuffle(List<Card> cards)
    {
        for (var i = cards.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
