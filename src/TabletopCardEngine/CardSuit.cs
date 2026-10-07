using System.ComponentModel;

namespace TabletopCardEngine;

/// <summary>A card's suit. No suit outranks another; the description is the display glyph.</summary>
public enum CardSuit : byte
{
    /// <summary>Clubs.</summary>
    [Description("♣")] Club = 0,

    /// <summary>Diamonds.</summary>
    [Description("♦")] Diamond = 1,

    /// <summary>Hearts.</summary>
    [Description("♥")] Heart = 2,

    /// <summary>Spades.</summary>
    [Description("♠")] Spade = 3
}
