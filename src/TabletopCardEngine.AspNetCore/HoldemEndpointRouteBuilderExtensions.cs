using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.AspNetCore.Http;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

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
                "Creates a new game with the supplied options. Returns 201 with GameData. May return 400 (malformed JSON), 409 (GameAlreadyExists), or 422 (InvalidGameOptions).",
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

        return group;
    }

    private static RouteHandlerBuilder WithOpenApiMetadata(
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
            .Produces<GameData>(successStatus)
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
