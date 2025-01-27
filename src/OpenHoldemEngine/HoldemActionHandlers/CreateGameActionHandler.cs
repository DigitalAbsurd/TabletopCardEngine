using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal sealed class CreateGameActionHandler : HoldemActionHandler<HoldemActionCreateGame>
{
    internal override async Task ImplementHandleAsync(HoldemActionCreateGame startGameActionData, HoldemDbContext dbContext)
    {
        dbContext.Add(new Game
        {
            Id = startGameActionData.GameId,
            AllowRebuy = startGameActionData.Options.AllowRebuy,
            AutoIncrementHands = startGameActionData.Options.AutoIncrementsHands,
            InitialSmallBlind = startGameActionData.Options.InitialSmallBlind,
            InitialDealerSeatNo = startGameActionData.Options.InitialDealerSeatNo,
            MaxHandsCount = startGameActionData.Options.MaxHandsCount,
            MaxPlayersCount = startGameActionData.Options.MaxPlayersCount
        });
        await dbContext.SaveChangesAsync();
    }
}
