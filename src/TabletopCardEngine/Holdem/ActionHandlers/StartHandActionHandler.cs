using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

internal sealed class StartHandActionHandler(ICardShuffler? cardShuffler) : HoldemActionHandler<HoldemActionStartHand>
{
    internal override async Task<HoldemError?> ImplementHandleAsync(HoldemActionStartHand startHandData, Game game, CancellationToken cancellationToken)
    {
        if (game.GetCurrentHand().No == 0)
        {
            return new HoldemError(HoldemErrorCode.GameNotStarted, PackResource.HoldemValidation_GameNotStarted);
        }

        if (!game.IsCurrentHandEnded())
        {
            return new HoldemError(HoldemErrorCode.PreviousHandNotEnded, PackResource.HoldemValidation_PreviousHandNotEnded);
        }

        var seatError = SeatValidation.Validate(game, startHandData.JoinSeats);
        if (seatError is not null)
        {
            return seatError;
        }

        await HandDealer.DealAsync(game, startHandData.JoinSeats, cardShuffler, cancellationToken);

        return null;
    }
}
