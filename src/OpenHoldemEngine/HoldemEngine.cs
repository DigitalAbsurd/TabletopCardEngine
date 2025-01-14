using OpenHoldemEngine.DTO;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine;

public class HoldemEngine(HoldemEngineOptions _options)
{
    public async Task<GameContext> GetGameContextAsync(Guid gameId)
    {
        using var dbContext = await _options.DbContextFactory.CreateDbContextAsync();
        var game = dbContext.Games.SingleOrDefault(x => x.Id == gameId);

        var gameContext = new GameContext(gameId, 
            ActionDetail: new HoldemActionDetailStartGame(_options.Settings.GamePlayersMinCount, _options.Settings.GamePlayersMaxCount));

        return gameContext;
    }
}