using FluentResults;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.HoldemActionHandlers;
using OpenHoldemEngine.Mappers;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine;

/// <summary>
/// The engine's whole public surface. There is no long-lived in-memory game: every call rehydrates
/// the aggregate from the database, applies one action, and projects the result back out.
/// <para>
/// Instances are obtained from the dependency-injection container registered by
/// <c>AddHoldemEngine</c>: <c>services.GetRequiredService&lt;HoldemEngine&gt;()</c>.
/// </para>
/// </summary>
public sealed class HoldemEngine
{
    private readonly HoldemEngineOptions _options;

    internal HoldemEngine(HoldemEngineOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Applies one action to a game and returns the game as it now stands.
    /// <para>
    /// A failed result short-circuits before the transaction is saved, so a validation failure
    /// leaves nothing persisted.
    /// </para>
    /// </summary>
    /// <param name="holdemAction">
    /// The action to apply. Its concrete record type decides which handler runs.
    /// </param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>
    /// The projected game on success, or a failed result carrying the validation messages.
    /// </returns>
    public async Task<Result<GameData>> ApplyHoldemActionAsync(HoldemAction holdemAction, CancellationToken cancellationToken = default)
    {
        IHoldemActionHandler holdemActionHandler = IHoldemActionHandler.CreateHandlerForAction(holdemAction, _options);

        using var dbContext = await _options.DbContextFactory.CreateDbContextAsync(cancellationToken);

        var game = await dbContext.LoadGameWithCurrentHandAsync(holdemAction.GameId);

        var result = await holdemActionHandler.HandleAsync(holdemAction, game);
        if (!result.IsSuccess)
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(GameDataMapper.ToGameData(game));
    }
}
