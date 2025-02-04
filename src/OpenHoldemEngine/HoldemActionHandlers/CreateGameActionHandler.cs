using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal sealed class CreateGameActionHandler : HoldemActionHandler<HoldemActionCreateGame>
{
    internal override Task ImplementHandleAsync(HoldemActionCreateGame startGameActionData, Game game)
    {
        game.Id = startGameActionData.GameId;
        game.AllowRebuy = startGameActionData.Options.AllowRebuy;
        game.AutoIncrementHands = startGameActionData.Options.AutoIncrementsHands;
        game.InitialSmallBlind = startGameActionData.Options.InitialSmallBlind;
        game.InitialDealerSeatNo = startGameActionData.Options.InitialDealerSeatNo;
        game.MaxHandsCount = startGameActionData.Options.MaxHandsCount;
        game.MaxPlayersCount = startGameActionData.Options.MaxPlayersCount;

        return Task.CompletedTask;
    }
}
