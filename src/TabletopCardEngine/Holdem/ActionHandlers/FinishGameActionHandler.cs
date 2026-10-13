using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

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
