using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal sealed class FinishGameActionHandler : HoldemActionHandler<HoldemActionFinishGame>
{
    internal override Task<HoldemError?> ImplementHandleAsync(HoldemActionFinishGame holdemAction, Game game, CancellationToken cancellationToken)
    {
        if (!game.IsCurrentHandEnded())
        {
            return Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.PreviousHandNotEnded, PackResource.HoldemValidation_PreviousHandNotEnded));
        }

        game.IsFinished = true;

        return Task.FromResult<HoldemError?>(null);
    }
}
