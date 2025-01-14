global using NCard = NPokerEngine.Types.Card;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.Converters;
internal class CardToNCardConverter : ValueConverter<Card, NCard>
{
    private static CardToStringConverter _cardToStringConverter = new CardToStringConverter();

    public CardToNCardConverter()
        : base(card => CardToNCard(card),
            nCard => NCardToCard(nCard))
    { }

    private static NCard CardToNCard(Card card)
        => NCard.FromString(new string(_cardToStringConverter.ConvertTo<string>(card).ToUpper().Reverse().ToArray()));

    private static Card NCardToCard(NCard nCard)
        => _cardToStringConverter.ConvertFrom<Card>(nCard.ToString());
}
