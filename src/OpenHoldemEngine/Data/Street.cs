using System.ComponentModel;

namespace OpenHoldemEngine.Data;

/// <summary>
/// A betting round.
/// <para>
/// The values are <b>not</b> sequential: <see cref="Flop"/>, <see cref="Turn"/> and
/// <see cref="River"/> encode how many community cards are on the board, and code relies on that.
/// Use <c>StreetExtensions.Next()</c> to move between streets rather than incrementing.
/// </para>
/// </summary>
[DefaultValue(PreFlop)]
public enum Street : byte
{
    /// <summary>Before the flop; no community cards.</summary>
    PreFlop = 0,

    /// <summary>Three community cards.</summary>
    Flop = 3,

    /// <summary>Four community cards.</summary>
    Turn = 4,

    /// <summary>Five community cards.</summary>
    River = 5,

    /// <summary>Betting is over and the remaining hands are compared.</summary>
    Showdown = 6,

    /// <summary>The hand is settled and the pot has been paid out.</summary>
    Finished = 7
}
