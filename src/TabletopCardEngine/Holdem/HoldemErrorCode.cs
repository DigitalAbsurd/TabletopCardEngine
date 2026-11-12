namespace TabletopCardEngine.Holdem;

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

    /// <summary>The game's <see cref="GameType"/> and <see cref="BettingStructure"/> is not a supported combination.</summary>
    UnsupportedGameOptions = 5,

    /// <summary>A game option is out of range: seat count, small blind or first dealer seat.</summary>
    InvalidGameOptions = 6,

    /// <summary>A seat to join is out of range, repeated, already occupied, or has a non-positive buy-in.</summary>
    InvalidSeat = 7,

    /// <summary>The game already has a hand dealt, or was already started, so it cannot be started again.</summary>
    GameAlreadyStarted = 8,

    /// <summary>No hand has been dealt yet, so the action needs <c>StartGame</c> first.</summary>
    GameNotStarted = 9,

    /// <summary>The game was finished, so only reading it back is allowed.</summary>
    GameFinished = 10,

    /// <summary>The game was changed by another action since the caller read it, so nothing was applied. Reload it and decide.</summary>
    Conflict = 11,

    /// <summary>No game with the requested id exists. Every action except <c>CreateGame</c> needs one.</summary>
    GameNotFound = 12,

    /// <summary>
    /// The action is not one the seat to act may take now, or its size is outside the window offered.
    /// <see cref="Dto.HandData.Evaluations"/> lists what is legal; nothing was applied.
    /// </summary>
    IllegalAction = 13,

    /// <summary>Fewer than two seats would have chips to play the hand: seat more players with <c>StartHand</c> and try again.</summary>
    NotEnoughPlayers = 14,

    /// <summary>A buy-in, top-up or rebuy amount is outside the game's buy-in window (<c>MinBuyIn</c>/<c>MaxBuyIn</c>), or would take the stack above <c>MaxBuyIn</c>.</summary>
    InvalidBuyIn = 15,

    /// <summary>The seat still has chips and the game does not allow topping up (<c>AllowTopUp</c> is off), so nothing was added.</summary>
    TopUpNotAllowed = 16,

    /// <summary>The seat has no chips and the game does not allow a rebuy (<c>AllowRebuy</c> is off), or the player has used up <c>MaxRebuys</c>.</summary>
    RebuyNotAllowed = 17,

    /// <summary>No tournament with the requested id exists. Every tournament action except <c>CreateTournament</c> needs one.</summary>
    TournamentNotFound = 18,

    /// <summary>A tournament with the requested id already exists.</summary>
    TournamentAlreadyExists = 19,

    /// <summary>A tournament option is out of range: the table format and seats, an amount, the entry limits, the levels or the payouts.</summary>
    InvalidTournamentOptions = 20,

    /// <summary>The tournament is no longer registering, so its entries cannot change.</summary>
    TournamentAlreadyStarted = 21,

    /// <summary>The tournament already has <c>MaxEntries</c> entries, so nobody else can register.</summary>
    TournamentFull = 22,

    /// <summary>The entry is invalid: a name that is empty, too long or already registered, or an entry number that is not registered.</summary>
    InvalidEntry = 23,
}
