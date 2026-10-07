namespace TabletopCardEngine;

/// <summary>
/// What the hand's ledger records for one action. It is <see cref="PlayerActionType"/> plus the two
/// forced blind posts, which the engine places itself and a caller can never request. The ledger
/// stores the action the caller asked for, so a request that the rules turned into something else
/// (an unaffordable call, say) is still recorded as requested.
/// </summary>
public enum HandActionType : byte
{
    /// <summary>The forced small-blind post, placed by the engine when a hand starts.</summary>
    SmallBlind = 1,

    /// <summary>The forced big-blind post, placed by the engine when a hand starts.</summary>
    BigBlind = 2,

    /// <summary>The seat gives up its claim on the pot.</summary>
    Fold = 3,

    /// <summary>The seat stays in without adding chips.</summary>
    Check = 4,

    /// <summary>The seat matches the standing wager.</summary>
    Call = 5,

    /// <summary>The seat raises the street wager to the recorded total.</summary>
    Raise = 6,

    /// <summary>An opening wager on a street where nothing has been bet.</summary>
    Bet = 8,

    /// <summary>The seat has committed its whole stack.</summary>
    AllIn = 9
}
