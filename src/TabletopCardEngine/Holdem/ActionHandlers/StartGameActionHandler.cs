using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <param name="cardShuffler">Supplies predetermined cards, or <see langword="null"/> to deal at random.</param>
/// <param name="level">The level a tournament table deals hand 1 at; <see langword="null"/> for a cash game.</param>
internal sealed class StartGameActionHandler(ICardShuffler? cardShuffler, DealLevel? level = null) : HoldemActionHandler<HoldemActionStartGame>
{
    internal override async Task<HoldemError?> ImplementHandleAsync(HoldemActionStartGame holdemAction, Game game, CancellationToken cancellationToken)
    {
        if (game.GetCurrentHand().No != 0)
        {
            return new HoldemError(HoldemErrorCode.GameAlreadyStarted, PackResource.HoldemValidation_GameAlreadyStarted);
        }

        var seatError = SeatValidation.Validate(game, holdemAction.JoinSeats);
        if (seatError is not null)
        {
            return seatError;
        }

        if (holdemAction.JoinSeats.Count < 2)
        {
            return new HoldemError(HoldemErrorCode.NotEnoughPlayers, PackResource.HoldemValidation_NotEnoughPlayers);
        }

        // The first hand resolves the previous dealer by walking the seats in order, so the order is
        // fixed here rather than left to every caller.
        var joinSeats = holdemAction.JoinSeats.OrderBy(join => join.SeatNo).ToArray();

        await HandDealer.DealAsync(game, joinSeats, cardShuffler, cancellationToken, level);

        return null;
    }
}
