namespace OpenHoldemEngine;

/// <summary>
/// Machine-readable reason an action was rejected. The numeric values are part of the contract:
/// they are explicit, never renumbered and never reused, so a caller may persist or switch on them.
/// There is deliberately no zero member, so <c>default</c> is never a valid code.
/// </summary>
public enum HoldemErrorCode
{
    /// <summary>A game with the requested id already exists.</summary>
    GameAlreadyExists = 1,

    /// <summary>The current hand is completed, so no player action can be applied to it.</summary>
    CurrentHandEnded = 2,

    /// <summary>The previous hand is not completed, so a new hand cannot start yet.</summary>
    PreviousHandNotEnded = 3,

    /// <summary>The player action's type is not a defined <see cref="PlayerActionType"/> member.</summary>
    InvalidAction = 4,

    /// <summary>The game's <see cref="GameType"/> or <see cref="BettingStructure"/> is not a defined, supported member.</summary>
    UnsupportedGameOptions = 5,

    /// <summary>A game option is out of range: seat count, small blind or first dealer seat.</summary>
    InvalidGameOptions = 6,

    /// <summary>A seat to join is out of range, repeated, already occupied, or has a non-positive buy-in.</summary>
    InvalidSeat = 7,
}
