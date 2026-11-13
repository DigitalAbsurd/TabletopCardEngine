using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.AspNetCore;

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

/// <summary>Carries the chips for a <c>POST /games/{gameId}/seats/{seatNo}/top-up</c> request.</summary>
public sealed record HoldemTopUpRequest
{
    /// <summary>The chips to add to the seat for the next hand, with at most two decimal places.</summary>
    public decimal Amount { get; init; }
}

/// <summary>Carries the options for a <c>POST /tournaments</c> request.</summary>
public sealed record HoldemCreateTournamentRequest
{
    /// <summary>The tournament rules. A level's <c>duration</c> is a <c>"hh:mm:ss"</c> string such as <c>"00:10:00"</c>. The levels have no default, so the request has to set them.</summary>
    public TournamentOptionData Options { get; init; } = new();

    /// <summary>The tournament ID to assign. Optional; the server generates a GUID when absent.</summary>
    public Guid? TournamentId { get; init; }
}

/// <summary>Carries the player for a <c>POST /tournaments/{tournamentId}/entries</c> request.</summary>
public sealed record HoldemRegisterRequest
{
    /// <summary>The player's display name, 1 to 64 characters, unique within the tournament regardless of case.</summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>Carries the optional parameters for a <c>POST /tournaments/{tournamentId}/start</c> request.</summary>
public sealed record HoldemStartTournamentRequest
{
    /// <summary>The seed of the seat draw, to reproduce one. Optional; a seed is drawn at random when absent.</summary>
    public int? SeatDrawSeed { get; init; }
}
