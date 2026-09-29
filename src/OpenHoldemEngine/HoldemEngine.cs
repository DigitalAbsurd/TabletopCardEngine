using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.HoldemActionHandlers;
using OpenHoldemEngine.Mappers;
using OpenHoldemEngine.Options;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine;

/// <summary>
/// The engine's whole public surface. There is no long-lived in-memory game: every call rehydrates
/// the aggregate from the database, applies one action, and projects the result back out.
/// <para>
/// <b>Scope of v1:</b> No-Limit Texas Hold'em cash games at 2 to 6 seats, with no antes and no rake.
/// Other structures (Pot-Limit, Fixed-Limit) are v2, and <see cref="GameOptionData"/> is where they will plug in.
/// </para>
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
    /// <para>
    /// <b>Concurrency:</b> <see cref="GameData.Version"/> counts the saves of a game, starting at <c>1</c>
    /// on creation; every call that saves increments it, and reading the game does not. The call fails with
    /// <see cref="HoldemErrorCode.Conflict"/> when <see cref="HoldemAction.ExpectedVersion"/> is set and no
    /// longer matches, or when another writer saved the game between this call's load and its save. Nothing
    /// is persisted on a conflict, and the engine never retries: the caller reloads with
    /// <see cref="HoldemAction.GetGame(Guid)"/> and decides.
    /// </para>
    /// </summary>
    /// <param name="holdemAction">
    /// The action to apply. Its concrete record type decides which handler runs.
    /// </param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>
    /// The projected game on success, or a failed result carrying the validation errors, each with a stable <see cref="HoldemErrorCode"/>.
    /// </returns>
    public Task<HoldemResult<GameData>> ApplyHoldemActionAsync(HoldemAction holdemAction, CancellationToken cancellationToken = default)
        => ApplyWithHookAsync(holdemAction, null, cancellationToken);

    /// <summary>The same call with a seam between handling the action and saving it, so a test can interleave two writers.</summary>
    internal async Task<HoldemResult<GameData>> ApplyWithHookAsync(HoldemAction holdemAction, Func<Task>? beforeSave, CancellationToken cancellationToken)
    {
        IHoldemActionHandler holdemActionHandler = IHoldemActionHandler.CreateHandlerForAction(holdemAction, _options);

        using var dbContext = await _options.DbContextFactory.CreateDbContextAsync(cancellationToken);

        var game = await dbContext.LoadGameWithCurrentHandAsync(holdemAction.GameId);

        var existed = game.Id != default;
        if (existed && holdemAction is not HoldemActionCreateGame && holdemAction.ExpectedVersion is { } expected && expected != game.Version)
        {
            return HoldemResult<GameData>.Fail(ConflictError());
        }

        var error = await holdemActionHandler.HandleAsync(holdemAction, game);
        if (error is not null)
        {
            return HoldemResult<GameData>.Fail(error);
        }

        if (holdemAction is HoldemActionGetGame)
        {
            return HoldemResult<GameData>.Ok(GameDataMapper.ToGameData(game));
        }

        if (existed)
        {
            game.Version++;
        }

        if (beforeSave is not null)
        {
            await beforeSave();
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return HoldemResult<GameData>.Fail(ConflictError());
        }

        return HoldemResult<GameData>.Ok(GameDataMapper.ToGameData(game));
    }

    private static HoldemError ConflictError()
        => new(HoldemErrorCode.Conflict, PackResource.HoldemValidation_Conflict);
}
