using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Engine;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <summary>
/// Removes a player at any time. Between hands it is <c>LeaveSeat</c>, through the same
/// <see cref="EntityExtensions.LeaveSeat"/>. During a hand the player leaves after it, and a seat still owing decisions
/// is folded at once, on the clock or not, through <see cref="HandRunner.FoldSeat"/>; a seat that has folded or is all
/// in needs no row, and an all-in one stays in the pot to showdown. The fold is never deferred to the seat's turn: that
/// would need a stored flag and an action the engine takes on its own.
/// </summary>
internal sealed class KickSeatActionHandler(ICardShuffler? cardShuffler) : HoldemActionHandler<HoldemActionKickSeat>
{
    internal override async Task<HoldemError?> ImplementHandleAsync(HoldemActionKickSeat holdemAction, Game game, CancellationToken cancellationToken)
    {
        if (game.GetCurrentHand().No == 0)
        {
            return new HoldemError(HoldemErrorCode.GameNotStarted, PackResource.HoldemValidation_GameNotStarted);
        }

        if (game.GetSeatedPlayer(holdemAction.SeatNo) is not { } player)
        {
            return new HoldemError(HoldemErrorCode.InvalidSeat, PackResource.HoldemValidation_InvalidSeat);
        }

        if (!game.IsCurrentHandEnded())
        {
            // A fold on the clock can close the street and deal the next one, so the deck is the one Play would use.
            var table = HandTableFactory.Build(game, await HandOutcomeRecorder.BuildDeckAsync(game, cardShuffler, cancellationToken));
            if (table.Find(holdemAction.SeatNo) is { IsWaiting: true })
            {
                var outcome = HandRunner.FoldSeat(table, holdemAction.SeatNo);
                HandOutcomeRecorder.Record(game, table, outcome, HandActionType.Fold, size: 0M);
            }
        }

        game.LeaveSeat(player);
        return null;
    }
}
