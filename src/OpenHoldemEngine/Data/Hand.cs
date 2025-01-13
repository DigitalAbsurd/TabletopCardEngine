namespace OpenHoldemEngine.Data;

public class Hand
{
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    public int No { get; set; }
    public List<HandDeckCard> DeckCards { get; set; } = new();
}