namespace OpenHoldemEngine;

/// <summary>
/// What a seat did. Only <see cref="Fold"/>, <see cref="Check"/>, <see cref="Call"/> and
/// <see cref="Raise"/> can be requested by a caller; the rest are recorded by the engine itself.
/// </summary>
public enum PlayerActionType : byte
{
    /// <summary>The forced small-blind post, placed by the engine when a hand starts.</summary>
    SmallBlind = 1,

    /// <summary>The forced big-blind post, placed by the engine when a hand starts.</summary>
    BigBlind = 2,

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

    /// <summary>A forced post other than a blind.</summary>
    Post = 7,

    /// <summary>An opening wager on a street where nothing has been bet.</summary>
    Bet = 8,

    /// <summary>The seat has committed its whole stack.</summary>
    AllIn = 9,

    /// <summary>The seat's cards were shown at showdown.</summary>
    Showdown = 100,

    /// <summary>The seat won a pot in this hand.</summary>
    Winner = 101,

    /// <summary>The seat won the game.</summary>
    GameWinner = 150,

    /// <summary>Chips were added to the seat's stack.</summary>
    BuyIn = 200
}
