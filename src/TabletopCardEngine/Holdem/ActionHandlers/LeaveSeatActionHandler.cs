using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <summary>
/// Stands a player up between hands. The <see cref="Player"/> row is kept and closed with the current
/// hand as its last one, because past hands' start states and actions still refer to the seat; the next
/// hand is then dealt without it, and the seat is free for a new <see cref="Player"/> row.
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

        var outOfRange = holdemAction.SeatNo < 1 || holdemAction.SeatNo > game.MaxPlayersCount;
        if (outOfRange || game.GetSeatLatestPlayer(holdemAction.SeatNo) is not { HandNoEnd: null } player)
        {
            return Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.InvalidSeat, PackResource.HoldemValidation_InvalidSeat));
        }

        // The current hand is the player's last one, not the next.
        player.HandNoEnd = currentHand.No;

        return Task.FromResult<HoldemError?>(null);
    }
}
