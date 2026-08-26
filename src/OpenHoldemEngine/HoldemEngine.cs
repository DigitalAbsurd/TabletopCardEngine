using FluentResults;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;
using OpenHoldemEngine.HoldemActionHandlers;
using OpenHoldemEngine.Mappers;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine;

public class HoldemEngine(HoldemEngineOptions _options)
{
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

        return Result.Ok(GameDataMapper.ToGameData(game));
    }
}
