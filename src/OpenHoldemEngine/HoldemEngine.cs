using OpenHoldemEngine.Converters;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;
using OpenHoldemEngine.HoldemActionHandlers;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine;

public class HoldemEngine(HoldemEngineOptions _options)
{
    private static readonly GameToGameDataConverter _gameToGameContextConverter = new GameToGameDataConverter();

    public async Task<GameData> ApplyHoldemActionAsync(HoldemAction holdemAction)
    {
        IHoldemActionHandler holdemActionHandler = holdemAction switch
        {
            HoldemActionCreateGame => new CreateGameActionHandler(),
            HoldemActionStartHand => new StartHandActionHandler(),
            _ => throw new NotImplementedException()
        };

        using var dbContext = await _options.DbContextFactory.CreateDbContextAsync();

        try
        {
            await dbContext.Database.BeginTransactionAsync();
            await holdemActionHandler.HandleAsync(holdemAction, dbContext);
            await dbContext.Database.CommitTransactionAsync();
        }
        catch (Exception)
        {
            await dbContext.Database.RollbackTransactionAsync();
            throw;
        }

        var game = await dbContext.LoadGameWitCurrentHandAsync(holdemAction.GameId);

        return _gameToGameContextConverter.ConvertTo<GameData>(game);
    }
}