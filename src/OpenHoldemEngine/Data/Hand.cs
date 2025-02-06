namespace OpenHoldemEngine.Data;

internal class Hand
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = new();
    public int No { get; set; }
    public byte DealerSeatNo { get; set; }

    public List<HandPlayerStartState> PlayerStartStates { get; set; } = new();
    public List<HandPlayerEndState> PlayerEndStates { get; set; } = new();
    public List<HandDeckCard> DeckCards { get; set; } = new();
    public List<HandHoleCard> HoleCards { get; set; } = new();
    public List<HandAction> Actions { get; set; } = new();
}