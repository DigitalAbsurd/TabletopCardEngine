namespace OpenHoldemEngine.Primitives;

public record Card
{
    public CardType CardType { get; init; }
    public CardSuit CardSuit { get; init; }
}