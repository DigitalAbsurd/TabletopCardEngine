namespace TabletopCardEngine.Holdem;

/// <summary>How a game's small blind is worked out each time its blind schedule raises a level.</summary>
public enum BlindIncreaseMode : byte
{
    /// <summary>Each level's small blind is the previous one times the schedule's multiplier, rounded to the cent. The default.</summary>
    Multiplier = 1,

    /// <summary>Each level's small blind is the next entry of the schedule's list of small blinds; the last one holds once the list is exhausted.</summary>
    Levels = 2,
}
