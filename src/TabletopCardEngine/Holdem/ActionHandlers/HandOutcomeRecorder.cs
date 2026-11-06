using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Engine;

namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <summary>
/// Writes what <see cref="HandRunner"/> did to a hand back as rows, for every handler that acts in a running hand
/// (<c>Play</c>, and <c>KickSeat</c> when it folds a seat): one ledger row, the board cards a street change dealt, and the
/// end states once the pot is awarded. One writer, so a fold reads the same in the ledger whoever asked for it.
/// </summary>
internal static class HandOutcomeRecorder
{
    /// <summary>
    /// The deck to rebuild the table with: the shuffler's remaining board cards, or <see langword="null"/> for a random
    /// deck when there is no shuffler or the board is complete. Asked before every street change, which is why a shuffler
    /// has to be idempotent within a hand.
    /// </summary>
    internal static async Task<CardDeck?> BuildDeckAsync(Game game, ICardShuffler? cardShuffler, CancellationToken cancellationToken)
    {
        var hand = game.GetCurrentHand();
        if (cardShuffler == null || (byte)hand.GetStreet() >= 5)
        {
            return null;
        }
        var deckCards = await cardShuffler.GetDeckCardsAsync(game.Id, hand.No, cancellationToken);
        var remaining = deckCards.Except(hand.DeckCards.Select(deckCard => deckCard.Card));
        return CardDeck.Predetermined(remaining);
    }

    /// <summary>Records <paramref name="outcome"/>, applied to <paramref name="table"/>, in the current hand.</summary>
    /// <param name="game">The game whose current hand the action was taken in.</param>
    /// <param name="table">The table the action was applied to, read for the hole cards and who reached showdown.</param>
    /// <param name="outcome">What the action did.</param>
    /// <param name="handActionType">What the ledger records: the action requested, not the rules' vocabulary.</param>
    /// <param name="size">The size the caller asked for.</param>
    internal static void Record(Game game, HandTable table, AppliedAction outcome, HandActionType handActionType, decimal size)
    {
        var hand = game.GetCurrentHand();

        game.AddAction(
            seatNo: outcome.SeatNo,
            street: outcome.Street,
            handActionType: handActionType,
            size: size,
            amount: outcome.Amount,
            addAmount: outcome.AddAmount,
            nextSeatNo: outcome.NextSeatNo,
            nextStreet: outcome.NextStreet,
            paid: outcome.Paid);

        foreach (var card in outcome.Board)
        {
            game.AddDeckCard(card);
        }

        if (outcome.PrizeBySeatNo is { } prizes)
        {
            var board = hand.GetDeckCards();
            var handEndStates = hand.PlayerStartStates.Select(startState =>
            {
                var hole = table.Find(startState.SeatNo)?.Hole ?? [];
                var seatBet = hand.GetSeatBetAmount(startState.SeatNo);
                var prize = prizes.GetValueOrDefault(startState.SeatNo);

                // Computed directly as prize - bet, not as (StartStack - bet + prize) - StartStack:
                // a deep stack (many integer digits) added to a prize with a long fractional tail
                // (an uneven pot split can need most of decimal's ~28-29 significant digits) can force
                // decimal to round the fraction away, and the two seats in a hand rarely have the same
                // number of stack digits — so the rounding wouldn't even land the same way on both
                // sides, and Sum(Net) across the hand would stop being exactly zero.
                var winAmount = prize - seatBet;

                var rankType = HandEvaluator.EvaluateRankType(hole, board);

                // Only a seat that is still in when the hand is shown down has a hand to describe: a
                // folded seat never showed its cards, and a pot won by everyone else folding is not shown.
                var reachedShowdown = table.ActiveCount > 1 && table.Find(startState.SeatNo)?.IsActive == true;

                return new HandPlayerEndState
                {
                    GameId = game.Id,
                    HandNo = hand.No,
                    Hand = hand,
                    SeatNo = startState.SeatNo,
                    FinishStack = startState.StartStack + winAmount,
                    WinAmount = winAmount,
                    Strength = HandEvaluator.EvaluateStrength(hole, board),
                    RankType = rankType,
                    Description = reachedShowdown ? HandEvaluator.Describe(rankType) : null,
                };
            });
            game.AddHandEndStates(handEndStates);
        }
    }
}
