using System.Globalization;

namespace OpenHoldemEngine.Data;

/// <summary>One playing card. Its string form is rank then suit, lowercase — see <see cref="ToString"/>.</summary>
public record Card
{
    private static readonly Dictionary<char, CardSuit> _suitByChar = new()
    {
        { 'c', CardSuit.Club },
        { 'd', CardSuit.Diamond },
        { 'h', CardSuit.Heart },
        { 's', CardSuit.Spade }
    };

    /// <summary>The card's rank.</summary>
    public CardType CardType { get; init; }

    /// <summary>The card's suit.</summary>
    public CardSuit CardSuit { get; init; }

    /// <summary>Rank then suit, lowercase — e.g. "ah" (ace of hearts), "tc" (ten of clubs), "2s".</summary>
    public override string ToString()
    {
        var typeByte = (byte)CardType;
        var typeChar = typeByte is >= 2 and <= 9 ? typeByte.ToString(CultureInfo.InvariantCulture) : $"{CardType.ToString().ToLowerInvariant()[0]}";
        var suitChar = CardSuit.ToString().ToLowerInvariant()[0];
        return $"{typeChar}{suitChar}";
    }

    /// <summary>
    /// Parses a card from either order — <c>"ah"</c> and <c>"ha"</c> both give the ace of hearts —
    /// case-insensitively.
    /// </summary>
    /// <param name="cardStr">A two-character card token.</param>
    /// <exception cref="NotImplementedException">The rank character is not recognised.</exception>
    public static Card FromString(string cardStr)
    {
        var typeChar = cardStr.ToLowerInvariant()[^1];
        var suitChar = cardStr.ToLowerInvariant()[0];

        if (_suitByChar.ContainsKey(typeChar))
        {
            (typeChar, suitChar) = (suitChar, typeChar);
        }

        var cardType = typeChar switch
        {
            char c when char.IsDigit(c) => (CardType)byte.Parse(c.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
            'a' => CardType.Ace,
            't' => CardType.Ten,
            'j' => CardType.Jack,
            'q' => CardType.Queen,
            'k' => CardType.King,
            _ => throw new NotImplementedException()
        };

        return new Card { CardType = cardType, CardSuit = _suitByChar[suitChar] };
    }
}
