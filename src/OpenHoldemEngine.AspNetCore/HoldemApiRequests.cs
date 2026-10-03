using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.AspNetCore;

/// <summary>Carries the optional parameters for a <c>POST /games</c> request.</summary>
public sealed record HoldemCreateGameRequest
{
    /// <summary>The game options. Optional; defaults to 6-seat, 1/2 blinds, no-limit hold'em.</summary>
    public GameOptionData Options { get; init; } = new();

    /// <summary>The game ID to assign. Optional; the server generates a GUID when absent.</summary>
    public Guid? GameId { get; init; }
}

/// <summary>Carries the players for a <c>POST /games/{gameId}/start</c> request.</summary>
public sealed record HoldemStartGameRequest
{
    /// <summary>The players joining the game, in any order.</summary>
    public IReadOnlyList<PlayerJoinData> JoinSeats { get; init; } = [];
}

/// <summary>Carries the optional parameters for a <c>POST /games/{gameId}/hands</c> request.</summary>
public sealed record HoldemStartHandRequest
{
    /// <summary>The seats to fill before dealing, for players who sit down between hands. Optional; defaults to an empty list to deal to the players already seated.</summary>
    public IReadOnlyList<PlayerJoinData> JoinSeats { get; init; } = [];
}

/// <summary>Carries the action for a <c>POST /games/{gameId}/actions</c> request.</summary>
/// <param name="ActionType">What the seat does.</param>
public sealed record HoldemPlayRequest(PlayerActionType ActionType)
{
    /// <summary>
    /// For a raise or a bet, the street total to raise or bet <i>to</i>; ignored for the other actions.
    /// </summary>
    public decimal Size { get; init; }
}
