using OpenHoldemEngine.Converters;

namespace OpenHoldemEngine.Data;

public record Card
{
    static readonly CardToStringConverter _cardToStringConverter = new CardToStringConverter();

    public CardType CardType { get; init; }
    public CardSuit CardSuit { get; init; }

    public override string ToString()
        => _cardToStringConverter.ConvertTo<string>(this);
}