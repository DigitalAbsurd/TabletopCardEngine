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

    /// <summary>Deals the first hand, seating the given players as it goes.</summary>
    /// <param name="gameId">The game to deal in.</param>
    /// <param name="joinSeats">
    /// The seats to fill, <b>in ascending seat order</b> — the first hand resolves the previous
    /// dealer by walking this array in order.
    /// </param>
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
/// Declared for completeness but not wired to a handler — <c>CreateHandlerForAction</c> does not
/// accept it. Seat players with <see cref="HoldemActionStartHand.JoinSeats"/> instead.
/// </summary>
/// <param name="GameId">The game to start.</param>
/// <param name="JoinSeats">The seats to fill.</param>
public record HoldemActionStartGame(Guid GameId, PlayerJoinData[] JoinSeats) : HoldemAction(GameId);

/// <summary>Deals the next hand, optionally seating, topping up or removing players first.</summary>
/// <param name="GameId">The game to deal in.</param>
public record HoldemActionStartHand(Guid GameId) : HoldemAction(GameId)
{
    /// <summary>
    /// Seats to fill before dealing, <b>in ascending seat order</b> — the very first hand resolves
    /// the previous dealer by walking this array in order.
    /// </summary>
    public PlayerJoinData[] JoinSeats { get; set; } = [];

    /// <summary>Top-ups to apply before dealing. Declared but not yet honored by any handler.</summary>
    public BuyInData[] BuyIns { get; set; } = [];

    /// <summary>Seats to empty before dealing. Declared but not yet honored by any handler.</summary>
    public byte[] KickOffSeats { get; set; } = [];
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
/// Declared for completeness but not wired to a handler — <c>CreateHandlerForAction</c> does not
/// accept it.
/// </summary>
/// <param name="GameId">The game to finish.</param>
public record HoldemActionFinishGame(Guid GameId) : HoldemAction(GameId);
