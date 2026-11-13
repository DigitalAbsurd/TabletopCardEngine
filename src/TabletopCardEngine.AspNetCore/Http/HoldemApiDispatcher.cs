using Microsoft.AspNetCore.Http;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.AspNetCore.Http;

/// <summary>
/// The one path every REST route goes through: it applies the If-Match rules, calls the engine,
/// and turns the result into an ETag header and a body, or a problem response.
/// </summary>
internal static class HoldemApiDispatcher
{
    /// <summary>
    /// Sends <paramref name="action"/> to the engine and answers with <paramref name="onSuccess"/> or a problem.
    /// Exceptions are not caught: a <see cref="HoldemSchemaException"/> reaches the host's exception handler.
    /// </summary>
    /// <param name="context">The request; its abort token cancels the database work.</param>
    /// <param name="engine">The engine to call.</param>
    /// <param name="action">The action to apply; its <c>ExpectedVersion</c> is taken from <c>If-Match</c>.</param>
    /// <param name="onSuccess">Builds the success response from the game the engine returns.</param>
    /// <param name="honourIfMatch">Whether <c>If-Match</c> is read at all; <see langword="false"/> for routes that have no version to compare.</param>
    internal static Task<IResult> SendAsync(HttpContext context, IHoldemTable engine, HoldemAction action,
        Func<GameData, IResult> onSuccess, bool honourIfMatch = true)
        => SendCoreAsync(context, action, (a, version) => a with { ExpectedVersion = version },
            engine.ApplyAsync, data => data.Version, onSuccess, honourIfMatch);

    /// <summary>
    /// Sends <paramref name="action"/> to the tournament engine and answers with <paramref name="onSuccess"/> or a problem,
    /// under the same If-Match and ETag rules as a game route, with the tournament's version as the tag.
    /// </summary>
    /// <param name="context">The request; its abort token cancels the database work.</param>
    /// <param name="engine">The tournament engine to call.</param>
    /// <param name="action">The action to apply; its <c>ExpectedVersion</c> is taken from <c>If-Match</c>.</param>
    /// <param name="onSuccess">Builds the success response from the tournament the engine returns.</param>
    /// <param name="honourIfMatch">Whether <c>If-Match</c> is read at all; <see langword="false"/> for routes that have no version to compare.</param>
    internal static Task<IResult> SendAsync(HttpContext context, IHoldemTournament engine, TournamentAction action,
        Func<TournamentData, IResult> onSuccess, bool honourIfMatch = true)
        => SendCoreAsync(context, action, (a, version) => a with { ExpectedVersion = version },
            engine.ApplyAsync, data => data.Version, onSuccess, honourIfMatch);

    private static async Task<IResult> SendCoreAsync<TAction, TData>(HttpContext context, TAction action,
        Func<TAction, int?, TAction> withExpectedVersion,
        Func<TAction, CancellationToken, Task<HoldemResult<TData>>> apply,
        Func<TData, int> versionOf, Func<TData, IResult> onSuccess, bool honourIfMatch)
    {
        var ifMatch = default(HoldemIfMatch);
        if (honourIfMatch)
        {
            ifMatch = HoldemETag.ParseIfMatch(context.Request.Headers.IfMatch);
            if (!ifMatch.Valid)
            {
                return TypedResults.Problem(
                    statusCode: 400,
                    detail: "If-Match must be * or a single strong entity tag such as \"3\".");
            }

            action = withExpectedVersion(action, ifMatch.ExpectedVersion);
        }

        var result = await apply(action, context.RequestAborted);
        if (!result.IsSuccess)
        {
            return HoldemProblems.ToProblem(result.Errors, ifMatch.Present);
        }

        context.Response.Headers.ETag = HoldemETag.Format(versionOf(result.Value));
        return onSuccess(result.Value);
    }
}
