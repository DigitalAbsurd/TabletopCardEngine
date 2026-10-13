using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Options;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

internal sealed class CreateGameActionHandler(HoldemSettings settings) : HoldemActionHandler<HoldemActionCreateGame>
{
    internal override Task<HoldemError?> ImplementHandleAsync(HoldemActionCreateGame startGameActionData, Game game, CancellationToken cancellationToken)
    {
        if (game.Id != default)
        {
            return Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.GameAlreadyExists, PackResource.HoldemValidation_GameAlreadyExists));
        }

        var optionsError = GameOptionsValidation.Validate(startGameActionData.Options, settings);
        if (optionsError is not null)
        {
            return Task.FromResult<HoldemError?>(optionsError);
        }

        game.Id = startGameActionData.GameId;
        game.Version = 1;
        game.InitialSmallBlind = startGameActionData.Options.InitialSmallBlind;
        game.InitialDealerSeatNo = startGameActionData.Options.InitialDealerSeatNo;
        game.GameType = startGameActionData.Options.GameType;
        game.BettingStructure = startGameActionData.Options.BettingStructure;
        game.MaxPlayersCount = startGameActionData.Options.MaxPlayersCount;

        return Task.FromResult<HoldemError?>(null);
    }
}
