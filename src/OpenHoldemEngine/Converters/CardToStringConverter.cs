using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.Converters;
internal class CardToStringConverter : ValueConverter<Card, string>
{
    public CardToStringConverter()
        : base(card => CardToString(card),
            strValue => CardFromString(strValue))
    {
    }

    private static string CardToString(Card card)
    {
        var cardTypeByte = (byte)card.CardType;
        var cardTypeChar = cardTypeByte >= 2 && cardTypeByte <= 9 ? cardTypeByte.ToString() : $"{card.CardType.ToString().ToLower().First()}";
        var cardSuitChar = $"{card.CardSuit.ToString().ToLower().First()}";
        return $"{cardTypeChar}{cardSuitChar}";
    }

    private static Card CardFromString(string strValue)
    {
        var cardTypeChar = strValue.ToLower().Last();
        var cardSuitChar = strValue.ToLower().First();
        var cardType = cardTypeChar switch
        {
            Char c when Char.IsDigit(c) => (CardType)byte.Parse(c.ToString()),
            'a' => CardType.Ace,
            't' => CardType.Ten,
            'j' => CardType.Jack,
            'q' => CardType.Queen,
            'k' => CardType.King,
            _ => throw new NotImplementedException()
        };

        var cardSuit = cardSuitChar switch
        {
            'c' => CardSuit.Club,
            'd' => CardSuit.Diamond,
            'h' => CardSuit.Heart,
            's' => CardSuit.Spade,
            _ => throw new NotImplementedException()
        };
        return new Card() { CardType = cardType, CardSuit = cardSuit };
    }
}
