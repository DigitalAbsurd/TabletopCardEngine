using System.Diagnostics;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Engine;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

internal sealed class PlayActionHandler(ICardShuffler? cardShuffler) : HoldemActionHandler<HoldemActionPlay>
{
    internal override async Task<HoldemError?> ImplementHandleAsync(HoldemActionPlay holdemAction, Game game, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(holdemAction.ActionType))
        {
            return new HoldemError(HoldemErrorCode.InvalidAction, PackResource.HoldemValidation_InvalidAction);
        }

        if (!AmountRules.HasValidScale(holdemAction.Size))
        {
            return new HoldemError(HoldemErrorCode.InvalidAction, PackResource.HoldemValidation_InvalidAction);
        }

        if (game.IsCurrentHandEnded())
        {
            return new HoldemError(HoldemErrorCode.CurrentHandEnded, PackResource.HoldemValidation_CurrentHandEnded);
        }

        var hand = game.GetCurrentHand();
        var table = HandTableFactory.Build(game, await BuildDeckAsync());

        var seatNo = table.NextSeatNo;
        var seat = table[seatNo];
        var window = BettingRules.LegalActions(table, seatNo);

        if (!IsOffered(BettingRules.Evaluate(table, seatNo)))
        {
            return new HoldemError(HoldemErrorCode.IllegalAction, PackResource.HoldemValidation_IllegalAction);
        }

        var (runnerAction, runnerAmount) = ToRunnerAction();
        var outcome = HandRunner.ApplyAction(table, runnerAction, runnerAmount);

        game.AddAction(
            seatNo: outcome.SeatNo,
            street: outcome.Street,
            handActionType: ToHandActionType(holdemAction.ActionType),
            size: holdemAction.Size,
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
            var board = game.GetCurrentHand().GetDeckCards();
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
                    GameId = holdemAction.GameId,
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

        return null;

        // Validated against the list the caller was offered, never against the window's own
        // arithmetic: that list has its special cases (Check versus Call, the synthetic all-in raise).
        // AllIn is always legal for the seat to act, so it is never rejected here.
        // A Bet is the opening wager of a street: legal only while nothing is wagered, and then only
        // inside the window the Raise is offered with, which is therefore the bet window.
        bool IsOffered(IReadOnlyList<PlayerActionEvaluationData> offered)
        {
            if (holdemAction.ActionType is PlayerActionType.AllIn)
            {
                return true;
            }
            if (holdemAction.ActionType is PlayerActionType.Bet && BettingRules.AgreeAmount(table) != 0)
            {
                return false;
            }
            var requested = holdemAction.ActionType is PlayerActionType.Bet ? PlayerActionType.Raise : holdemAction.ActionType;
            var entry = offered.FirstOrDefault(evaluation => evaluation.PlayerActionType == requested);
            if (entry is null)
            {
                return false;
            }
            return requested != PlayerActionType.Raise
                || (entry.MinValue <= holdemAction.Size && holdemAction.Size <= entry.MaxValue);
        }

        // The one place a request becomes something HandRunner understands, which speaks only Fold,
        // Call and Raise. AllIn commits the whole stack: a call (possibly for less) when that does not
        // go past the call amount, otherwise a raise to the full stack, full or short.
        (PlayerActionType Action, decimal Amount) ToRunnerAction()
        {
            if (holdemAction.ActionType is PlayerActionType.AllIn)
            {
                var allInTo = seat.Stack + seat.StreetWager;
                return allInTo <= window.CallTo
                    ? (PlayerActionType.Call, window.CallTo)
                    : (PlayerActionType.Raise, allInTo);
            }
            return (holdemAction.ActionType, holdemAction.ActionType switch
            {
                PlayerActionType.Check => seat.StreetWager,
                PlayerActionType.Call => window.CallTo,
                PlayerActionType.Fold => 0M,
                PlayerActionType.Raise => holdemAction.Size,
                PlayerActionType.Bet => holdemAction.Size,
                _ => throw new UnreachableException()
            });
        }

        // The one place a request becomes a ledger entry: every PlayerActionType is also a HandActionType.
        static HandActionType ToHandActionType(PlayerActionType actionType)
            => actionType switch
            {
                PlayerActionType.Fold => HandActionType.Fold,
                PlayerActionType.Check => HandActionType.Check,
                PlayerActionType.Call => HandActionType.Call,
                PlayerActionType.Raise => HandActionType.Raise,
                PlayerActionType.Bet => HandActionType.Bet,
                PlayerActionType.AllIn => HandActionType.AllIn,
                _ => throw new UnreachableException()
            };

        async Task<CardDeck?> BuildDeckAsync()
        {
            if (cardShuffler == null || (byte)hand.GetStreet() >= 5)
            {
                return null;
            }
            var deckCards = await cardShuffler.GetDeckCardsAsync(holdemAction.GameId, hand.No, cancellationToken);
            var remaining = deckCards.Except(hand.DeckCards.Select(deckCard => deckCard.Card));
            return CardDeck.Predetermined(remaining);
        }
    }
}
