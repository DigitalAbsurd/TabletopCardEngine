namespace TabletopCardEngine.Holdem.Data;

internal class Hand
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = new();
    public int No { get; set; }
    public byte DealerSeatNo { get; set; }

    /// <summary>The small blind this hand was dealt at; <see langword="null"/> means the game's initial small blind, which is what every hand written before blind schedules has.</summary>
    public decimal? SmallBlind { get; set; }

    /// <summary>The ante this hand was dealt with; <see langword="null"/> on a hand without an ante and on every hand dealt before 1.4.</summary>
    public decimal? Ante { get; set; }

    /// <summary>How many levels the blinds have risen by when this hand is dealt.</summary>
    public int BlindLevel { get; set; }

    /// <summary>The number of the hand at which the current blind level began.</summary>
    public int BlindLevelStartHandNo { get; set; }

    /// <summary>How many of the schedule's player thresholds have already fired.</summary>
    public int BlindThresholdsPassed { get; set; }

    public List<HandPlayerStartState> PlayerStartStates { get; set; } = [];
    public List<HandPlayerEndState> PlayerEndStates { get; set; } = [];
    public List<HandDeckCard> DeckCards { get; set; } = [];
    public List<HandHoleCard> HoleCards { get; set; } = [];
    public List<HandAction> Actions { get; set; } = [];
}
