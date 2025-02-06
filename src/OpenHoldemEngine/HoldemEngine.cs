using FluentResults;
using OpenHoldemEngine.Converters;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;
using OpenHoldemEngine.HoldemActionHandlers;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine;

public class HoldemEngine(HoldemEngineOptions _options)
{
    private static readonly GameToGameDataConverter _gameToGameContextConverter = new GameToGameDataConverter();

    public async Task<Result<GameData>> ApplyHoldemActionAsync(HoldemAction holdemAction, CancellationToken cancellationToken = default)
    {
        IHoldemActionHandler holdemActionHandler = IHoldemActionHandler.CreateHandlerForAction(holdemAction);

        using var dbContext = await _options.DbContextFactory.CreateDbContextAsync();

        var game = await dbContext.LoadGameWithCurrentHandAsync(holdemAction.GameId);

        var result = await holdemActionHandler.HandleAsync(holdemAction, game);
        if (!result.IsSuccess)
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(_gameToGameContextConverter.ConvertTo<GameData>(game));
    }
}