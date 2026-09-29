namespace OpenHoldemEngine.Dto;

/// <summary>
/// The one command type the engine accepts. Which concrete record you pass decides which handler
/// runs — see <c>IHoldemActionHandler.CreateHandlerForAction</c>. A new action type has to be added
/// both here and to that switch.
/// </summary>
/// <param name="GameId">The game the action applies to.</param>
public abstract record HoldemAction(Guid GameId)
{
    /// <summary>Creates a game with the given rules, generating an identifier when none is supplied.</summary>
    /// <param name="optionData">The rules the game is created with.</param>
    /// <param name="gameId">Identifier to use, or <see langword="null"/> to generate one.</param>
    public static HoldemActionCreateGame CreateCreateGameDto(GameOptionData optionData, Guid? gameId)
        => new(gameId ?? Guid.NewGuid(), optionData);

    /// <summary>Deals the next hand of a started game, seating the given players first.</summary>
    /// <param name="gameId">The game to deal in.</param>
    /// <param name="joinSeats">The seats to fill before dealing, for players who sit down between hands.</param>
    public static HoldemActionStartHand CreateStartHandDto(Guid gameId, PlayerJoinData[] joinSeats)
        => new(gameId) { JoinSeats = joinSeats };

    /// <summary>Deals the next hand to the players already seated.</summary>
    /// <param name="gameId">The game to deal in.</param>
    public static HoldemActionStartHand CreateStartNextHandDto(Guid gameId)
        => new(gameId);

    /// <summary>Plays one action for the seat whose turn it is.</summary>
    /// <param name="gameId">The game to act in.</param>
    /// <param name="actionType">What the seat does.</param>
    /// <param name="size">
    /// For a raise, the street total to raise <i>to</i>; ignored for actions that carry no size.
    /// </param>
    public static HoldemActionPlayerAction CreatePlayerActionDto(Guid gameId, PlayerActionType actionType, decimal size = 0)
        => new(gameId, actionType, size);
}

/// <summary>Reads the game back without changing anything.</summary>
/// <param name="GameId">The game to read.</param>
public record HoldemActionGetGame(Guid GameId) : HoldemAction(GameId);

/// <summary>Creates a game. Fails if one with this identifier already exists.</summary>
/// <param name="GameId">Identifier for the new game.</param>
/// <param name="Options">The rules the game is created with.</param>
public record HoldemActionCreateGame(Guid GameId, GameOptionData Options) : HoldemAction(GameId);

/// <summary>
/// Starts a created game: seats the players and deals hand 1. Allowed once, on a game with no hand
/// dealt yet; otherwise it fails with <see cref="HoldemErrorCode.GameAlreadyStarted"/>.
/// </summary>
/// <param name="GameId">The game to start.</param>
/// <param name="JoinSeats">The seats to fill, in any order; the engine sorts them by seat number.</param>
public record HoldemActionStartGame(Guid GameId, PlayerJoinData[] JoinSeats) : HoldemAction(GameId);

/// <summary>
/// Deals the next hand, optionally seating or topping up players first. Fails with
/// <see cref="HoldemErrorCode.GameNotStarted"/> until <see cref="HoldemActionStartGame"/> has dealt hand 1.
/// </summary>
/// <param name="GameId">The game to deal in.</param>
public record HoldemActionStartHand(Guid GameId) : HoldemAction(GameId)
{
    /// <summary>Seats to fill before dealing, for players who sit down between hands.</summary>
    public PlayerJoinData[] JoinSeats { get; set; } = [];
}

/// <summary>Plays one action for the seat whose turn it is.</summary>
/// <param name="GameId">The game to act in.</param>
/// <param name="ActionType">What the seat does.</param>
/// <param name="Size">
/// For <see cref="PlayerActionType.Raise"/>, the street total to raise <i>to</i> — the same absolute
/// basis as the raise bounds in <see cref="HandData.Evaluations"/>. Ignored for other actions.
/// </param>
public record HoldemActionPlayerAction(Guid GameId, PlayerActionType ActionType, decimal Size) : HoldemAction(GameId);

/// <summary>
/// Closes the game, between hands only: it fails with <see cref="HoldemErrorCode.PreviousHandNotEnded"/>
/// while a hand is in progress. Afterwards every action except <see cref="HoldemActionGetGame"/> fails with
/// <see cref="HoldemErrorCode.GameFinished"/>.
/// </summary>
/// <param name="GameId">The game to finish.</param>
public record HoldemActionFinishGame(Guid GameId) : HoldemAction(GameId);
