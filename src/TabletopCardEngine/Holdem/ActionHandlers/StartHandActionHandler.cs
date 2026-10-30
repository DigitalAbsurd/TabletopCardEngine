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

        var seatsWithChips = game.GetCurrentHand().PlayerEndStates
            .Where(endState => endState.FinishStack > 0 && game.IsSeatTaken(endState.SeatNo))
            .Select(endState => endState.SeatNo)
            .ToHashSet();
        foreach (var joinData in startHandData.JoinSeats)
        {
            seatsWithChips.Add(joinData.SeatNo);
        }

        if (seatsWithChips.Count < 2)
        {
            return new HoldemError(HoldemErrorCode.NotEnoughPlayers, PackResource.HoldemValidation_NotEnoughPlayers);
        }

        await HandDealer.DealAsync(game, startHandData.JoinSeats, cardShuffler, cancellationToken);

        return null;
    }
}
