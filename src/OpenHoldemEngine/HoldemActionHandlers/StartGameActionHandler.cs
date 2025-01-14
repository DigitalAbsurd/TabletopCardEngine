using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal sealed class StartGameActionHandler : HoldemActionHandler<HoldemActionStartGame>
{
    internal override async Task ImplementHandleAsync(HoldemActionStartGame startGameActionData, HoldemDbContext dbContext)
    {
        dbContext.Add(new Game
        {
            Id = startGameActionData.GameId,
            AllowRebuy = startGameActionData.Options.AllowRebuy,
            AutoIncrementHands = startGameActionData.Options.AutoIncrementsHands,
            InitialSmallBlind = startGameActionData.Options.InitialSmallBlind,
            MaxHandsCount = startGameActionData.Options.MaxHandsCount,
            MaxPlayersCount = startGameActionData.Options.MaxPlayersCount
        });
        await dbContext.SaveChangesAsync();

        var startHandData = new HoldemActionStartHand(startGameActionData.GameId)
        {
            JoinSeats = startGameActionData.Seats,
            BuyIns = startGameActionData.BuyIns
        };
        await new StartHandActionHandler().HandleAsync(startHandData, dbContext);
    }
}
