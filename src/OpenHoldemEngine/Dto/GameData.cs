namespace OpenHoldemEngine.Dto;

/// <summary>A single action a seat has taken, as recorded in the hand's ledger.</summary>
/// <param name="Street">The street the action was taken on.</param>
/// <param name="ActionType">What the seat did.</param>
/// <param name="Size">The size the caller asked for; <c>0</c> for actions that carry no size.</param>
/// <param name="No">Position of this action in the hand, ascending from 1.</param>
public record PlayerActionData(Street Street, HandActionType ActionType, decimal Size, int No);

/// <summary>One seat as it stands in the current hand.</summary>
/// <param name="SeatNo">The seat number. Seats are 1-based; <c>0</c> means "no seat".</param>
/// <param name="Name">Display name of the player in the seat.</param>
/// <param name="StartHandStack">The stack this seat sat down with at the start of the hand.</param>
/// <param name="Stack">The stack right now — <paramref name="StartHandStack"/> minus everything wagered this hand.</param>
/// <param name="BetSize">The seat's wager on the current street, when it has one.</param>
/// <param name="Hole">The seat's hole cards, or <see langword="null"/> when they are not visible to the caller.</param>
/// <param name="LastAction">The most recent action this seat took, or <see langword="null"/> if it has not acted.</param>
/// <param name="HandResult">Set once the hand is over; <see langword="null"/> while it is still running.</param>
public record SeatData(
    byte SeatNo,
    string Name,
    decimal StartHandStack,
    decimal Stack,
    decimal? BetSize = null,
    Card[]? Hole = null,
    PlayerActionData? LastAction = null,
    HandResultData? HandResult = null)
{
    /// <summary>Stacks are decimal, so "no chips left" is a comparison against this, never against zero.</summary>
    private const decimal DecimalMinPrecision = 0.0001M;

    /// <summary>Whether the seat is still contesting the pot — it has chips and cards, or it is all in.</summary>
    public bool IsActive
        => (Stack > DecimalMinPrecision && Hole?.Length > 0 && LastAction?.ActionType != HandActionType.Fold)
        || (Stack < DecimalMinPrecision && LastAction != null && LastAction.ActionType != HandActionType.Fold);

    /// <summary>Whether the seat has folded out of the hand.</summary>
    public bool IsFolded => LastAction?.ActionType == HandActionType.Fold;

    /// <summary>Whether the seat has committed its whole stack.</summary>
    public bool IsAllIn => LastAction != default && Stack < DecimalMinPrecision;
}

/// <summary>The current hand: who is in it, what is on the board, and what the seat to act may do.</summary>
/// <param name="No">Hand number within the game, ascending from 1.</param>
/// <param name="Seats">Every occupied seat, ascending by seat number.</param>
/// <param name="DealerSeatNo">Where the button is. Seats are 1-based; <c>0</c> means "no dealer yet".</param>
/// <param name="SmallBlindSeatNo">The seat that posted the small blind, or <c>0</c> before the hand starts.</param>
/// <param name="BigBlindSeatNo">The seat that posted the big blind, or <c>0</c> before the hand starts.</param>
/// <param name="NextPlayerSeatNo">The seat to act, or <c>0</c> when it is nobody's turn.</param>
/// <param name="Street">The street now in progress.</param>
/// <param name="StreetPot">Contributions made on the current street only.</param>
/// <param name="Pot">Everything contributed on prior streets — <paramref name="StreetPot"/> is not included.</param>
/// <param name="CommunityCards">The board, in dealt order.</param>
/// <param name="Evaluations">What <paramref name="NextPlayerSeatNo"/> may legally do; empty when it is nobody's turn.</param>
public record HandData(
    int No,
    SeatData[] Seats,
    byte DealerSeatNo,
    byte SmallBlindSeatNo,
    byte BigBlindSeatNo,
    byte NextPlayerSeatNo,
    Street Street,
    decimal StreetPot,
    decimal Pot,
    Card[] CommunityCards,
    PlayerActionEvaluationData[] Evaluations)
{
    /// <summary>The most recent action taken by anyone in this hand.</summary>
    public PlayerActionData GetLastAction()
        => Seats.Where(seat => seat.LastAction is not null)
            .Select(seat => seat.LastAction!)
            .OrderBy(action => action.No)
            .Last();

    /// <summary>The lower bound of the sizing window for <paramref name="actionType"/>.</summary>
    internal decimal GetEvalMinValue(PlayerActionType actionType)
        => Evaluations.Single(evaluation => evaluation.PlayerActionType == actionType).MinValue;

    /// <summary>The upper bound of the sizing window for <paramref name="actionType"/>.</summary>
    internal decimal GetEvalMaxValue(PlayerActionType actionType)
        => Evaluations.Single(evaluation => evaluation.PlayerActionType == actionType).MaxValue;
}

/// <summary>Everything the engine knows about a game, as returned by every call.</summary>
/// <param name="GameId">Identifier of the game.</param>
/// <param name="Options">The rules the game was created with.</param>
/// <param name="HandData">The current hand, or <see langword="null"/> when no hand has been dealt yet.</param>
/// <param name="IsFinished">Whether <c>FinishGame</c> has closed the game; after that only reading it back is allowed.</param>
/// <param name="Version">
/// How many times the game has been saved, starting at <c>1</c> on creation. Pass it back as
/// <see cref="HoldemAction.ExpectedVersion"/> to have a stale write rejected with <see cref="HoldemErrorCode.Conflict"/>.
/// Reading the game does not change it.
/// </param>
public record GameData(Guid GameId, GameOptionData Options, HandData? HandData, bool IsFinished = false, int Version = 0);
