namespace TabletopCardEngine.Holdem;

/// <summary>
/// The table format of a No-Limit game, chosen when the game is created: how many seats the table
/// is built for. It belongs to the game and is stored with it.
/// </summary>
public enum HoldemTableFormat : byte
{
    /// <summary>Heads-Up (2-Max): one-on-one play, exactly 2 seats.</summary>
    HeadsUp = 1,

    /// <summary>6-Max: a short-handed table of 2 to 6 seats. The default format.</summary>
    SixMax = 2,

    /// <summary>Full Ring: the traditional full-size table of 7 to 10 seats, most often nine.</summary>
    FullRing = 3,
}
