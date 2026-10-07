using TabletopCardEngine.Data;
using TabletopCardEngine.Dto;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.HoldemActionHandlers;

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
