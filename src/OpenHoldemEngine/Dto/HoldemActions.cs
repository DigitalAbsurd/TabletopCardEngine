namespace OpenHoldemEngine.Dto;

/// <summary>
/// The one command type the engine accepts. Which concrete record you pass decides which handler
/// runs — see <c>IHoldemActionHandler.CreateHandlerForAction</c>. A new action type has to be added
/// both here and to that switch. Every command is a <c>HoldemAction&lt;Verb&gt;</c> record with one
/// static factory of the same verb on this type. The shape rule for the records: required data is
/// positional, optional data is an <see langword="init"/> property.
/// </summary>
/// <param name="GameId">The game the action applies to.</param>
public abstract record HoldemAction(Guid GameId)
{
    /// <summary>
    /// The <see cref="GameData.Version"/> the caller last saw, or <see langword="null"/> to skip the check.
    /// When set and different from the game's current version, the call fails with
    /// <see cref="HoldemErrorCode.Conflict"/> before anything runs. Ignored by <see cref="HoldemActionCreateGame"/>,
    /// which has no game to compare against.
    /// </summary>
    public int? ExpectedVersion { get; init; }

    /// <summary>Reads the game back without changing anything.</summary>
    /// <param name="gameId">The game to read.</param>
    public static HoldemActionGetGame GetGame(Guid gameId)
        => new(gameId);

    /// <summary>Creates a game with the given rules, generating an identifier when none is supplied.</summary>
    /// <param name="options">The rules the game is created with.</param>
    /// <param name="gameId">Identifier to use, or <see langword="null"/> to generate one.</param>
    public static HoldemActionCreateGame CreateGame(GameOptionData options, Guid? gameId = null)
        => new(gameId ?? Guid.NewGuid(), options);

    /// <summary>Starts a created game: seats the players and deals hand 1.</summary>
    /// <param name="gameId">The game to start.</param>
    /// <param name="joinSeats">The seats to fill, in any order.</param>
    public static HoldemActionStartGame StartGame(Guid gameId, PlayerJoinData[] joinSeats)
        => new(gameId, joinSeats);

    /// <summary>Deals the next hand of a started game, optionally seating players first.</summary>
    /// <param name="gameId">The game to deal in.</param>
    /// <param name="joinSeats">
    /// The seats to fill before dealing, for players who sit down between hands, or
    /// <see langword="null"/> to deal to the players already seated.
    /// </param>
    public static HoldemActionStartHand StartHand(Guid gameId, PlayerJoinData[]? joinSeats = null)
        => new(gameId) { JoinSeats = joinSeats ?? [] };

    /// <summary>Plays one action for the seat whose turn it is.</summary>
    /// <param name="gameId">The game to act in.</param>
    /// <param name="actionType">What the seat does.</param>
    /// <param name="size">
    /// For a raise, the street total to raise <i>to</i>; ignored for actions that carry no size.
    /// </param>
    public static HoldemActionPlay Play(Guid gameId, PlayerActionType actionType, decimal size = 0)
        => new(gameId, actionType, size);

    /// <summary>Closes the game, between hands only.</summary>
    /// <param name="gameId">The game to finish.</param>
    public static HoldemActionFinishGame FinishGame(Guid gameId)
        => new(gameId);
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
/// <param name="JoinSeats">The seats to fill, in any order; the engine sorts them by seat number. Any read-only list will do; the engine only reads it.</param>
public record HoldemActionStartGame(Guid GameId, IReadOnlyList<PlayerJoinData> JoinSeats) : HoldemAction(GameId);

/// <summary>
/// Deals the next hand, optionally seating or topping up players first. Fails with
/// <see cref="HoldemErrorCode.GameNotStarted"/> until <see cref="HoldemActionStartGame"/> has dealt hand 1.
/// </summary>
/// <param name="GameId">The game to deal in.</param>
public record HoldemActionStartHand(Guid GameId) : HoldemAction(GameId)
{
    /// <summary>Seats to fill before dealing, for players who sit down between hands. Any read-only list will do; the engine only reads it.</summary>
    public IReadOnlyList<PlayerJoinData> JoinSeats { get; init; } = [];
}

/// <summary>Plays one action for the seat whose turn it is.</summary>
/// <param name="GameId">The game to act in.</param>
/// <param name="ActionType">What the seat does.</param>
/// <param name="Size">
/// For <see cref="PlayerActionType.Raise"/>, the street total to raise <i>to</i> — the same absolute
/// basis as the raise bounds in <see cref="HandData.Evaluations"/>. Ignored for other actions.
/// </param>
public record HoldemActionPlay(Guid GameId, PlayerActionType ActionType, decimal Size) : HoldemAction(GameId);

/// <summary>
/// Closes the game, between hands only: it fails with <see cref="HoldemErrorCode.PreviousHandNotEnded"/>
/// while a hand is in progress. Afterwards every action except <see cref="HoldemActionGetGame"/> fails with
/// <see cref="HoldemErrorCode.GameFinished"/>.
/// </summary>
/// <param name="GameId">The game to finish.</param>
public record HoldemActionFinishGame(Guid GameId) : HoldemAction(GameId);
