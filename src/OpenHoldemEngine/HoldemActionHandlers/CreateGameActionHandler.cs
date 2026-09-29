using FluentResults;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal sealed class CreateGameActionHandler : HoldemActionHandler<HoldemActionCreateGame>
{
    internal override Task<Result> ImplementHandleAsync(HoldemActionCreateGame startGameActionData, Game game)
    {
        if (game.Id != default)
        {
            return Task.FromResult(Result.Fail(PackResource.HoldemValidation_GameAlreadyExists));
        }

        game.Id = startGameActionData.GameId;
        game.InitialSmallBlind = startGameActionData.Options.InitialSmallBlind;
        game.InitialDealerSeatNo = startGameActionData.Options.InitialDealerSeatNo;
        game.MaxPlayersCount = startGameActionData.Options.MaxPlayersCount;

        return Task.FromResult(Result.Ok());
    }
}
