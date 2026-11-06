using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <summary>
/// Stands a player up between hands. The seat is vacated by <see cref="EntityExtensions.LeaveSeat"/>, shared with
/// <c>KickSeat</c>, which may also run during a hand.
/// </summary>
internal sealed class LeaveSeatActionHandler : HoldemActionHandler<HoldemActionLeaveSeat>
{
    internal override Task<HoldemError?> ImplementHandleAsync(HoldemActionLeaveSeat holdemAction, Game game, CancellationToken cancellationToken)
    {
        var currentHand = game.GetCurrentHand();
        if (currentHand.No == 0)
        {
            return Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.GameNotStarted, PackResource.HoldemValidation_GameNotStarted));
        }

        if (!game.IsCurrentHandEnded())
        {
            return Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.PreviousHandNotEnded, PackResource.HoldemValidation_PreviousHandNotEnded));
        }

        if (game.GetSeatedPlayer(holdemAction.SeatNo) is not { } player)
        {
            return Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.InvalidSeat, PackResource.HoldemValidation_InvalidSeat));
        }

        game.LeaveSeat(player);
        return Task.FromResult<HoldemError?>(null);
    }
}
