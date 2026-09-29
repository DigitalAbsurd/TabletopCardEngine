using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Resources;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal sealed class StartGameActionHandler(ICardShuffler? cardShuffler) : HoldemActionHandler<HoldemActionStartGame>
{
    internal override async Task<HoldemError?> ImplementHandleAsync(HoldemActionStartGame holdemAction, Game game, CancellationToken cancellationToken)
    {
        if (game.Id == default)
        {
            return new HoldemError(HoldemErrorCode.GameNotStarted, PackResource.HoldemValidation_GameNotStarted);
        }

        if (game.GetCurrentHand().No != 0)
        {
            return new HoldemError(HoldemErrorCode.GameAlreadyStarted, PackResource.HoldemValidation_GameAlreadyStarted);
        }

        var seatError = SeatValidation.Validate(game, holdemAction.JoinSeats);
        if (seatError is not null)
        {
            return seatError;
        }

        // The first hand resolves the previous dealer by walking the seats in order, so the order is
        // fixed here rather than left to every caller.
        var joinSeats = holdemAction.JoinSeats.OrderBy(join => join.SeatNo).ToArray();

        await HandDealer.DealAsync(game, joinSeats, cardShuffler, cancellationToken);

        return null;
    }
}
