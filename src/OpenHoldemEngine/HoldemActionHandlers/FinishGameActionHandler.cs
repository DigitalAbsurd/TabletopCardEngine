using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal sealed class FinishGameActionHandler : HoldemActionHandler<HoldemActionFinishGame>
{
    internal override Task<HoldemError?> ImplementHandleAsync(HoldemActionFinishGame holdemAction, Game game)
    {
        if (game.Id == default)
        {
            return Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.GameNotStarted, PackResource.HoldemValidation_GameNotStarted));
        }

        if (!game.IsCurrentHandEnded())
        {
            return Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.PreviousHandNotEnded, PackResource.HoldemValidation_PreviousHandNotEnded));
        }

        game.IsFinished = true;

        return Task.FromResult<HoldemError?>(null);
    }
}
