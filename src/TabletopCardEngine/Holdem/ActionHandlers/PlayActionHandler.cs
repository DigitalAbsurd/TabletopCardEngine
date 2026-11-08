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

        var table = HandTableFactory.Build(game, await HandOutcomeRecorder.BuildDeckAsync(game, cardShuffler, cancellationToken));

        var seatNo = table.NextSeatNo;
        var seat = table[seatNo];
        var window = BettingRules.LegalActions(table, seatNo);

        if (!IsOffered(BettingRules.Evaluate(table, seatNo)))
        {
            return new HoldemError(HoldemErrorCode.IllegalAction, PackResource.HoldemValidation_IllegalAction);
        }

        var (runnerAction, runnerAmount) = ToRunnerAction();
        var outcome = HandRunner.ApplyAction(table, runnerAction, runnerAmount);

        HandOutcomeRecorder.Record(game, table, outcome, ToHandActionType(holdemAction.ActionType), holdemAction.Size);

        return null;

        // Validated against the list the caller was offered, never against the window's own
        // arithmetic: that list has its special cases (Check versus Call, the synthetic all-in raise).
        // AllIn is legal as a call (possibly for less) whenever the seat is to act; as a raise it is
        // legal only where raising is, which a short all-in or the Fixed-Limit cap may have closed to this
        // seat, and only up to the largest raise the structure allows, so a Pot-Limit all-in over the pot
        // and a Fixed-Limit all-in past the one raise-to total are rejected.
        // A Bet is the opening wager of a street: legal only while nothing is wagered, and then only
        // inside the window the Raise is offered with, which is therefore the bet window.
        bool IsOffered(IReadOnlyList<PlayerActionEvaluationData> offered)
        {
            if (holdemAction.ActionType is PlayerActionType.AllIn)
            {
                var allInTo = seat.Stack + seat.StreetWager;
                return allInTo <= window.CallTo
                    || (BettingRules.IsRaisingOpen(table, seatNo)
                        && allInTo <= BettingRules.MaxRaiseAmount(table, seatNo));
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

    }
}
