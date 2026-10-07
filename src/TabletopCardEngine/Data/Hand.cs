namespace TabletopCardEngine.Data;

internal class Hand
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = new();
    public int No { get; set; }
    public byte DealerSeatNo { get; set; }

    public List<HandPlayerStartState> PlayerStartStates { get; set; } = [];
    public List<HandPlayerEndState> PlayerEndStates { get; set; } = [];
    public List<HandDeckCard> DeckCards { get; set; } = [];
    public List<HandHoleCard> HoleCards { get; set; } = [];
    public List<HandAction> Actions { get; set; } = [];
}
