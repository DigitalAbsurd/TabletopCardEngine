using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal sealed class CreateGameActionHandler : HoldemActionHandler<HoldemActionCreateGame>
{
    internal override Task<HoldemError?> ImplementHandleAsync(HoldemActionCreateGame startGameActionData, Game game)
    {
        if (game.Id != default)
        {
            return Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.GameAlreadyExists, PackResource.HoldemValidation_GameAlreadyExists));
        }

        game.Id = startGameActionData.GameId;
        game.InitialSmallBlind = startGameActionData.Options.InitialSmallBlind;
        game.InitialDealerSeatNo = startGameActionData.Options.InitialDealerSeatNo;
        game.MaxPlayersCount = startGameActionData.Options.MaxPlayersCount;

        return Task.FromResult<HoldemError?>(null);
    }
}
