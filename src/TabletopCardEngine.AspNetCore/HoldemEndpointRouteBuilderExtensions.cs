using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.AspNetCore.Http;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.AspNetCore;

/// <summary>Maps the engine's opt-in REST API into an ASP.NET Core host.</summary>
public static class HoldemEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the engine's REST API under <paramref name="prefix"/> and returns the group, so the host can chain
    /// <c>RequireAuthorization()</c>, rate limiting and the like onto every route. Nothing is mapped unless this is called.
    /// <para>
    /// The API returns every seat's hole cards, as <see cref="GameData"/> does: protect the group.
    /// </para>
    /// </summary>
    /// <param name="endpoints">The route builder to map into.</param>
    /// <param name="prefix">The path the routes are grouped under.</param>
    /// <returns>The route group holding every route of the API.</returns>
    /// <exception cref="InvalidOperationException">
    /// The services were not registered with <c>AddHoldem(configuration, ...)</c> of this package.
    /// </exception>
    public static RouteGroupBuilder MapHoldemApi(this IEndpointRouteBuilder endpoints, string prefix = "/api/holdem")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        if (endpoints.ServiceProvider.GetService<HoldemApiMarker>() is null)
        {
            throw new InvalidOperationException(
                "MapHoldemApi requires services.AddTabletopCardEngine(tce => tce.AddHoldem(configuration, ...)) from TabletopCardEngine.AspNetCore, " +
                "which registers the engine and the JSON contract. Call it before building the application.");
        }

        var group = endpoints.MapGroup(prefix);

        group.MapPost("/games", (HoldemCreateGameRequest request, HttpContext context, IHoldemTable engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.CreateGame(request.Options, request.GameId),
                    data => TypedResults.CreatedAtRoute(data, "HoldemGetGame", new { gameId = data.GameId }),
                    honourIfMatch: false))
            .WithName("HoldemCreateGame")
            .WithOpenApiMetadata(
                "Creates a game.",
                "Creates a new game with the supplied options. options.bettingStructure is NoLimit, PotLimit or FixedLimit (a string; a number is refused), and options.raiseCap applies to FixedLimit. options.ante (null for none) is the ante every hand posts and options.anteType is Classic (every seat dealt in posts it) or BigBlind (the big blind seat posts it alone), a string; a number is refused. Returns 201 with GameData. May return 400 (malformed JSON), 409 (GameAlreadyExists), or 422 (InvalidGameOptions).",
                StatusCodes.Status201Created,
                [StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict, StatusCodes.Status422UnprocessableEntity]);

        group.MapPost("/games/{gameId:guid}/start", (HoldemStartGameRequest? request, Guid gameId, HttpContext context, IHoldemTable engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.StartGame(gameId, [.. (request?.JoinSeats ?? [])]),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemStartGame")
            .WithOpenApiMetadata(
                "Seats the players and deals hand 1.",
                "Seats the supplied players and deals the first hand. Returns 200 with GameData. May return 400 (bad If-Match), 404 (GameNotFound), 409 (GameAlreadyStarted, NotEnoughPlayers, PreviousHandNotEnded), 412 (Conflict), or 422 (InvalidSeat).",
                StatusCodes.Status200OK,
                [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status422UnprocessableEntity]);

        group.MapGet("/games/{gameId:guid}", (Guid gameId, HttpContext context, IHoldemTable engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.GetGame(gameId), data => TypedResults.Ok(data)))
            .WithName("HoldemGetGame")
            .WithOpenApiMetadata(
                "Reads a game.",
                "Reads the current state of a game by ID. Returns 200 with GameData. May return 400 (bad If-Match), 404 (GameNotFound), or 412 (Conflict).",
                StatusCodes.Status200OK,
                [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed]);

        group.MapPost("/games/{gameId:guid}/hands", (HoldemStartHandRequest? request, Guid gameId, HttpContext context, IHoldemTable engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.StartHand(gameId, [.. (request?.JoinSeats ?? [])]),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemStartHand")
            .WithOpenApiMetadata(
                "Deals the next hand.",
                "Deals the next hand in the game, optionally seating new players. Returns 200 with GameData. May return 400 (bad If-Match), 404 (GameNotFound), 409 (GameNotStarted, NotEnoughPlayers, PreviousHandNotEnded), 412 (Conflict), or 422 (InvalidAction).",
                StatusCodes.Status200OK,
                [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status422UnprocessableEntity]);

        group.MapPost("/games/{gameId:guid}/actions", (HoldemPlayRequest request, Guid gameId, HttpContext context, IHoldemTable engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.Play(gameId, request.ActionType, request.Size),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemPlay")
            .WithOpenApiMetadata(
                "Plays one action for the seat to act.",
                "Plays one action for the seat whose turn it is. Returns 200 with GameData. May return 400 (bad If-Match, malformed JSON, numeric enum), 404 (GameNotFound), 409 (GameFinished), 412 (Conflict), or 422 (IllegalAction).",
                StatusCodes.Status200OK,
                [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status422UnprocessableEntity]);

        group.MapPost("/games/{gameId:guid}/finish", (Guid gameId, HttpContext context, IHoldemTable engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.FinishGame(gameId),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemFinishGame")
            .WithOpenApiMetadata(
                "Closes the game between hands.",
                "Closes the game between hands. Returns 200 with GameData. May return 400 (bad If-Match), 404 (GameNotFound), 409 (PreviousHandNotEnded), or 412 (Conflict).",
                StatusCodes.Status200OK,
                [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed]);

        group.MapPost("/games/{gameId:guid}/seats/{seatNo:int}/leave", async (Guid gameId, int seatNo, HttpContext context, IHoldemTable engine) =>
        {
            if (seatNo < 1 || seatNo > 255)
            {
                return TypedResults.Problem(
                    statusCode: 422,
                    detail: "Seat number must be between 1 and 255.");
            }

            return await HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.LeaveSeat(gameId, (byte)seatNo),
                data => TypedResults.Ok(data));
        })
            .WithName("HoldemLeaveSeat")
            .WithOpenApiMetadata(
                "Stands a player up between hands.",
                "Stands a player up between hands. Returns 200 with GameData. May return 400 (bad If-Match), 404 (GameNotFound), 409 (GameNotStarted, PreviousHandNotEnded, GameFinished), 412 (Conflict), or 422 (InvalidSeat).",
                StatusCodes.Status200OK,
                [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status422UnprocessableEntity]);

        group.MapPost("/games/{gameId:guid}/seats/{seatNo:int}/top-up", async (HoldemTopUpRequest request, Guid gameId, int seatNo, HttpContext context, IHoldemTable engine) =>
        {
            if (seatNo < 1 || seatNo > 255)
            {
                return TypedResults.Problem(
                    statusCode: 422,
                    detail: "Seat number must be between 1 and 255.");
            }

            return await HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.TopUp(gameId, (byte)seatNo, request.Amount),
                data => TypedResults.Ok(data));
        })
            .WithName("HoldemTopUpSeat")
            .WithOpenApiMetadata(
                "Adds chips to a seat between hands.",
                "Adds chips to an occupied seat for the next hand, or rebuys a seat with no chips left. Returns 200 with GameData. May return 400 (bad If-Match, malformed JSON), 404 (GameNotFound), 409 (GameNotStarted, PreviousHandNotEnded, TopUpNotAllowed, RebuyNotAllowed, GameFinished), 412 (Conflict), or 422 (InvalidSeat, InvalidBuyIn).",
                StatusCodes.Status200OK,
                [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status422UnprocessableEntity]);

        group.MapPost("/games/{gameId:guid}/seats/{seatNo:int}/kick", async (Guid gameId, int seatNo, HttpContext context, IHoldemTable engine) =>
        {
            if (seatNo < 1 || seatNo > 255)
            {
                return TypedResults.Problem(
                    statusCode: 422,
                    detail: "Seat number must be between 1 and 255.");
            }

            return await HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.KickSeat(gameId, (byte)seatNo),
                data => TypedResults.Ok(data));
        })
            .WithName("HoldemKickSeat")
            .WithOpenApiMetadata(
                "Removes a player at any time.",
                "Removes a player between hands or during a hand, where the seat folds at once. Returns 200 with GameData. May return 400 (bad If-Match), 404 (GameNotFound), 409 (GameNotStarted, GameFinished), 412 (Conflict), or 422 (InvalidSeat).",
                StatusCodes.Status200OK,
                [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status422UnprocessableEntity]);

        int[] tournamentProblems = [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status422UnprocessableEntity];

        group.MapPost("/tournaments", (HoldemCreateTournamentRequest request, HttpContext context, IHoldemTournament engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, TournamentAction.CreateTournament(request.Options, request.TournamentId),
                    data => TypedResults.CreatedAtRoute(data, "HoldemGetTournament", new { tournamentId = data.TournamentId }),
                    honourIfMatch: false))
            .WithName("HoldemCreateTournament")
            .WithOpenApiMetadata<TournamentData>(
                "Creates a tournament.",
                "Creates a tournament in the Registering state with the supplied options. options.levels needs 1 to 100 blind levels; a level's duration is a \"hh:mm:ss\" string such as \"00:10:00\", at least one minute. options.bettingStructure, options.tableFormat and options.anteType are strings; a number is refused. options.payouts are percentages summing to 100 and default to [100]. Returns 201 with TournamentData and a Location header. May return 400 (malformed JSON), 409 (TournamentAlreadyExists), or 422 (UnsupportedGameOptions, InvalidTournamentOptions).",
                StatusCodes.Status201Created,
                [StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict, StatusCodes.Status422UnprocessableEntity]);

        group.MapGet("/tournaments/{tournamentId:guid}", (Guid tournamentId, HttpContext context, IHoldemTournament engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, TournamentAction.GetTournament(tournamentId), data => TypedResults.Ok(data)))
            .WithName("HoldemGetTournament")
            .WithOpenApiMetadata<TournamentData>(
                "Reads a tournament.",
                "Reads the current state of a tournament by ID; the ETag is its version. Returns 200 with TournamentData. May return 400 (bad If-Match), 404 (TournamentNotFound), or 412 (Conflict).",
                StatusCodes.Status200OK,
                [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed]);

        group.MapPost("/tournaments/{tournamentId:guid}/entries", (HoldemRegisterRequest request, Guid tournamentId, HttpContext context, IHoldemTournament engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, TournamentAction.Register(tournamentId, request.Name),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemRegisterEntry")
            .WithOpenApiMetadata<TournamentData>(
                "Registers a player.",
                "Registers a player while the tournament is Registering; the new entry's entryNo is in the returned entries. Returns 200 with TournamentData. May return 400 (bad If-Match, malformed JSON), 404 (TournamentNotFound), 409 (TournamentAlreadyStarted, TournamentFull), 412 (Conflict), or 422 (InvalidEntry).",
                StatusCodes.Status200OK,
                tournamentProblems);

        group.MapDelete("/tournaments/{tournamentId:guid}/entries/{entryNo:int}", (Guid tournamentId, int entryNo, HttpContext context, IHoldemTournament engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, TournamentAction.Unregister(tournamentId, entryNo),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemUnregisterEntry")
            .WithOpenApiMetadata<TournamentData>(
                "Withdraws a registration.",
                "Withdraws a registration while the tournament is Registering. Returns 200 with TournamentData. May return 400 (bad If-Match), 404 (TournamentNotFound), 409 (TournamentAlreadyStarted), 412 (Conflict), or 422 (InvalidEntry).",
                StatusCodes.Status200OK,
                tournamentProblems);

        group.MapPost("/tournaments/{tournamentId:guid}/start", (HoldemStartTournamentRequest? request, Guid tournamentId, HttpContext context, IHoldemTournament engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, TournamentAction.StartTournament(tournamentId, request?.SeatDrawSeed),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemStartTournament")
            .WithOpenApiMetadata<TournamentData>(
                "Starts a tournament.",
                "Closes registration, draws the seats, opens the tables as games and deals their first hands; each table's gameId is in the returned tables. Players act on a table with the game routes, which take actions and reads only. Returns 200 with TournamentData. May return 400 (bad If-Match, malformed JSON), 404 (TournamentNotFound), 409 (TournamentAlreadyStarted, NotEnoughPlayers), or 412 (Conflict).",
                StatusCodes.Status200OK,
                tournamentProblems);

        group.MapPost("/tournaments/{tournamentId:guid}/tables/{tableNo:int}/deal", (Guid tournamentId, int tableNo, HttpContext context, IHoldemTournament engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, TournamentAction.DealTable(tournamentId, tableNo),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemDealTable")
            .WithOpenApiMetadata<TournamentData>(
                "Deals a table's next hand.",
                "Records the eliminations of a table whose hand has ended and deals its next hand, or finishes the tournament when one player is left. Returns 200 with TournamentData. May return 400 (bad If-Match), 404 (TournamentNotFound), 409 (TournamentNotStarted, TournamentFinished, PreviousHandNotEnded), 412 (Conflict), or 422 (InvalidTable).",
                StatusCodes.Status200OK,
                tournamentProblems);

        return group;
    }

    private static RouteHandlerBuilder WithOpenApiMetadata(
        this RouteHandlerBuilder builder,
        string summary,
        string description,
        int successStatus,
        int[] problemStatuses)
        => builder.WithOpenApiMetadata<GameData>(summary, description, successStatus, problemStatuses);

    private static RouteHandlerBuilder WithOpenApiMetadata<TData>(
        this RouteHandlerBuilder builder,
        string summary,
        string description,
        int successStatus,
        int[] problemStatuses)
    {
        return builder
            .WithTags("Holdem")
            .WithSummary(summary)
            .WithDescription(description)
            .Produces<TData>(successStatus)
            .WithProblemsForStatuses(problemStatuses);
    }

    private static RouteHandlerBuilder WithProblemsForStatuses(
        this RouteHandlerBuilder builder,
        int[] statusCodes)
    {
        foreach (var status in statusCodes)
        {
            builder = builder.ProducesProblem(status);
        }

        return builder;
    }
}
