namespace TabletopCardEngine.Holdem;

/// <summary>
/// What a caller may ask a seat to do. The engine also records the forced blind posts, but those
/// are never requested, so they live only in <see cref="HandActionType"/>.
/// </summary>
public enum PlayerActionType : byte
{
    /// <summary>The seat gives up its claim on the pot.</summary>
    Fold = 3,

    /// <summary>The seat stays in without adding chips; legal only when there is nothing to call.</summary>
    Check = 4,

    /// <summary>The seat matches the standing wager.</summary>
    Call = 5,

    /// <summary>
    /// The seat raises the street wager. Its size is the total to raise <i>to</i>, not the increment.
    /// </summary>
    Raise = 6,

    /// <summary>An opening wager on a street where nothing has been bet.</summary>
    Bet = 8,

    /// <summary>Puts the seat's whole stack in: a call when the stack does not exceed the amount to call, otherwise a raise to the full stack.</summary>
    AllIn = 9
}
