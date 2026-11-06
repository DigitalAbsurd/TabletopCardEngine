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

        // No next hand will deal them, so top-ups and rebuys still waiting were never in play.
        foreach (var pending in game.GetPendingBuyIns())
        {
            game.BuyIns.Remove(pending);
        }

        return Task.FromResult<HoldemError?>(null);
    }
}
