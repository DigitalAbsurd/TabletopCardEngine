using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.AspNetCore.Http;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.AspNetCore;

/// <summary>Maps the engine's opt-in REST API into an ASP.NET Core host.</summary>
public static class HoldemEngineEndpointRouteBuilderExtensions
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
    /// The services were not registered with <c>AddHoldemEngine(configuration, ...)</c> of this package.
    /// </exception>
    public static RouteGroupBuilder MapHoldemEngineApi(this IEndpointRouteBuilder endpoints, string prefix = "/api/holdem")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        if (endpoints.ServiceProvider.GetService<HoldemEngineApiMarker>() is null)
        {
            throw new InvalidOperationException(
                "MapHoldemEngineApi requires services.AddHoldemEngine(configuration, ...) from OpenHoldemEngine.AspNetCore, " +
                "which registers the engine and the JSON contract. Call it before building the application.");
        }

        var group = endpoints.MapGroup(prefix);

        group.MapPost("/games", (HoldemCreateGameRequest request, HttpContext context, HoldemEngine engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.CreateGame(request.Options, request.GameId),
                    data => TypedResults.CreatedAtRoute(data, "HoldemGetGame", new { gameId = data.GameId }),
                    honourIfMatch: false))
            .WithName("HoldemCreateGame");

        group.MapPost("/games/{gameId:guid}/start", (HoldemStartGameRequest? request, Guid gameId, HttpContext context, HoldemEngine engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.StartGame(gameId, [.. (request?.JoinSeats ?? [])]),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemStartGame");

        group.MapGet("/games/{gameId:guid}", (Guid gameId, HttpContext context, HoldemEngine engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.GetGame(gameId), data => TypedResults.Ok(data)))
            .WithName("HoldemGetGame");

        group.MapPost("/games/{gameId:guid}/hands", (HoldemStartHandRequest? request, Guid gameId, HttpContext context, HoldemEngine engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.StartHand(gameId, [.. (request?.JoinSeats ?? [])]),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemStartHand");

        group.MapPost("/games/{gameId:guid}/actions", (HoldemPlayRequest request, Guid gameId, HttpContext context, HoldemEngine engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.Play(gameId, request.ActionType, request.Size),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemPlay");

        group.MapPost("/games/{gameId:guid}/finish", (Guid gameId, HttpContext context, HoldemEngine engine) =>
                HoldemApiDispatcher.SendAsync(context, engine, HoldemAction.FinishGame(gameId),
                    data => TypedResults.Ok(data)))
            .WithName("HoldemFinishGame");

        return group;
    }
}
