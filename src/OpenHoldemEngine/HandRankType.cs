namespace OpenHoldemEngine;

/// <summary>
/// The display category of a made hand, ascending in strength. This is for showing the result only —
/// two hands of the same category are ranked against each other by their strength value, not by this.
/// </summary>
public enum HandRankType : byte
{
    /// <summary>No hand has been evaluated.</summary>
    None = 0,

    /// <summary>No pair or better.</summary>
    HighCard = 1,

    /// <summary>One pair.</summary>
    Pair = 2,

    /// <summary>Two pair.</summary>
    TwoPair = 3,

    /// <summary>Three of a kind.</summary>
    Trips = 4,

    /// <summary>Five cards in sequence.</summary>
    Straight = 5,

    /// <summary>Five cards of one suit.</summary>
    Flush = 6,

    /// <summary>Three of a kind plus a pair.</summary>
    FullHouse = 7,

    /// <summary>Four of a kind.</summary>
    FourOfAKind = 8,

    /// <summary>Five suited cards in sequence.</summary>
    StraightFlush = 9
}
