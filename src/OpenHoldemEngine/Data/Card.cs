namespace OpenHoldemEngine.Data;

public record Card
{
    private static readonly Dictionary<char, CardSuit> SuitByChar = new()
    {
        { 'c', CardSuit.Club },
        { 'd', CardSuit.Diamond },
        { 'h', CardSuit.Heart },
        { 's', CardSuit.Spade }
    };

    public CardType CardType { get; init; }
    public CardSuit CardSuit { get; init; }

    /// <summary>Rank then suit, lowercase — e.g. "ah" (ace of hearts), "tc" (ten of clubs), "2s".</summary>
    public override string ToString()
    {
        var typeByte = (byte)CardType;
        var typeChar = typeByte is >= 2 and <= 9 ? typeByte.ToString() : $"{CardType.ToString().ToLowerInvariant()[0]}";
        var suitChar = CardSuit.ToString().ToLowerInvariant()[0];
        return $"{typeChar}{suitChar}";
    }

    public static Card FromString(string cardStr)
    {
        var typeChar = cardStr.ToLowerInvariant()[^1];
        var suitChar = cardStr.ToLowerInvariant()[0];

        if (SuitByChar.ContainsKey(typeChar))
        {
            (typeChar, suitChar) = (suitChar, typeChar);
        }

        var cardType = typeChar switch
        {
            char c when char.IsDigit(c) => (CardType)byte.Parse(c.ToString()),
            'a' => CardType.Ace,
            't' => CardType.Ten,
            'j' => CardType.Jack,
            'q' => CardType.Queen,
            'k' => CardType.King,
            _ => throw new NotImplementedException()
        };

        return new Card { CardType = cardType, CardSuit = SuitByChar[suitChar] };
    }
}
