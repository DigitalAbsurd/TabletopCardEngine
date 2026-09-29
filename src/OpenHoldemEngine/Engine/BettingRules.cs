namespace OpenHoldemEngine.Engine;

/// <summary>What a seat may legally do right now. A null raise bound means raising is not possible.</summary>
internal readonly record struct ActionEvaluations(decimal CallTo, decimal? MinRaiseTo, decimal? MaxRaiseTo);

/// <summary>
/// Legal-action and bet-sizing rules, ported from NPokerEngine's <c>ActionChecker</c>. Pure
/// functions over a <see cref="HandTable"/> — nothing here is stateful.
/// </summary>
internal static class BettingRules
{
    private static readonly HandActionType[] _raiseKinds =
        [HandActionType.Raise, HandActionType.SmallBlind, HandActionType.BigBlind];

    /// <summary>The street total a call brings a seat up to: the biggest raise-kind wager so far this street.</summary>
    internal static decimal AgreeAmount(HandTable table) => LastRaise(table)?.Amount ?? 0M;

    /// <summary>
    /// The smallest legal "raise to" total: the last raise's total plus its own increment (so the
    /// big blind's post — amount 2·sb, increment sb — makes the preflop minimum raise 3·sb, not
    /// the 4·sb a naive "double the bet" rule would give). Never less than one big blind, whether or
    /// not a raise has been made: a blind posted by a stack too short to cover it is a raise-kind
    /// entry below a full blind, and a raise can never bring the street total under one.
    /// </summary>
    internal static decimal MinRaiseAmount(HandTable table)
    {
        var lastRaise = LastRaise(table);
        return lastRaise is { } raise
            ? Math.Max(raise.Amount + raise.AddAmount, table.SmallBlind * 2)
            : table.SmallBlind * 2;
    }

    internal static bool IsAllIn(TableSeat seat, PlayerActionType action, decimal betAmount)
        => action switch
        {
            // A call that would exceed the stack is an all-in call for less; a raise is only
            // all-in when it lands exactly on the seat's remaining chips.
            PlayerActionType.Call => betAmount >= seat.Stack + seat.StreetWager,
            PlayerActionType.Raise => betAmount == seat.Stack + seat.StreetWager,
            _ => false
        };

    /// <summary>An action the rules cannot honor is silently turned into a fold, exactly as NPokerEngine does.</summary>
    internal static (PlayerActionType Action, decimal Amount) CorrectAction(
        HandTable table, byte seatNo, PlayerActionType action, decimal amount)
    {
        var seat = table[seatNo];
        if (IsAllIn(seat, action, amount))
        {
            return (action, seat.Stack + seat.StreetWager);
        }
        if (IsIllegal(table, seatNo, action, amount))
        {
            return (PlayerActionType.Fold, 0M);
        }
        return (action, amount);
    }

    internal static bool IsIllegal(HandTable table, byte seatNo, PlayerActionType action, decimal amount)
    {
        var seat = table[seatNo];
        return action switch
        {
            PlayerActionType.Fold => false,
            PlayerActionType.Call => IsShortOfMoney(seat, amount) || amount != AgreeAmount(table),
            PlayerActionType.Raise => IsShortOfMoney(seat, amount) || MinRaiseAmount(table) > amount,
            _ => false
        };
    }

    private static bool IsShortOfMoney(TableSeat seat, decimal amount) => seat.Stack < amount - seat.StreetWager;

    internal static ActionEvaluations LegalActions(HandTable table, byte seatNo)
    {
        var seat = table[seatNo];
        var minRaise = MinRaiseAmount(table);
        var maxRaise = seat.Stack + seat.StreetWager;
        return maxRaise < minRaise
            ? new ActionEvaluations(AgreeAmount(table), null, null)
            : new ActionEvaluations(AgreeAmount(table), minRaise, maxRaise);
    }

    /// <summary>
    /// The biggest raise-kind wager this street, scanning seats ascending and, within a seat,
    /// chronologically — the same flattened order NPokerEngine's <c>FetchLastRaise</c> scans, so a
    /// tie (a short all-in raise landing exactly on the standing bet) resolves to the same entry.
    /// </summary>
    private static (decimal Amount, decimal AddAmount)? LastRaise(HandTable table)
    {
        (decimal Amount, decimal AddAmount)? best = null;
        foreach (var seat in table.Seats.OrderBy(seat => seat.SeatNo))
        {
            foreach (var bet in seat.StreetBets)
            {
                if (Array.IndexOf(_raiseKinds, bet.Action) < 0)
                {
                    continue;
                }
                if (best is null || bet.Amount > best.Value.Amount)
                {
                    best = (bet.Amount, bet.AddAmount);
                }
            }
        }
        return best;
    }
}
