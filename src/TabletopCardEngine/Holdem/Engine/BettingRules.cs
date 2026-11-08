using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Holdem.Engine;

/// <summary>What a seat may legally do right now. A null raise bound means raising is not possible.</summary>
internal readonly record struct ActionEvaluations(decimal CallTo, decimal? MinRaiseTo, decimal? MaxRaiseTo);

/// <summary>
/// Legal-action and bet-sizing rules, ported from NPokerEngine's <c>ActionChecker</c>. Pure
/// functions over a <see cref="HandTable"/> — nothing here is stateful.
/// </summary>
internal static class BettingRules
{
    private static readonly HandActionType[] _raiseKinds =
        [HandActionType.Raise, HandActionType.Bet, HandActionType.SmallBlind, HandActionType.BigBlind];

    /// <summary>The street total a call brings a seat up to: the biggest raise-kind wager so far this street.</summary>
    internal static decimal AgreeAmount(HandTable table) => LastRaise(table)?.Amount ?? 0M;

    /// <summary>
    /// The smallest legal "raise to" total: the standing wager plus the full-raise increment. The
    /// increment is the largest one among this street's full bets and raises, and never less than one
    /// big blind. A bet or raise, an all-in raise included, is full when its increment reaches the
    /// increment standing when it was made; an incomplete all-in raise lifts the standing wager and
    /// leaves the increment where it was. Blinds set no increment, so pre-flop it starts at one big
    /// blind and the first minimum raise is to two. Pre-flop the standing wager is never counted
    /// below the nominal big blind, so a blind posted by a stack too short to cover it does not lower
    /// the minimum.
    /// </summary>
    internal static decimal MinRaiseAmount(HandTable table)
    {
        var (standing, increment) = StandingAndIncrement(table, int.MaxValue);
        return table.BettingStructure == BettingStructure.FixedLimit ? standing + FixedBetSize(table) : standing + increment;
    }

    /// <summary>
    /// The one Fixed-Limit bet: the small bet, the hand's big blind, pre-flop and on the flop; the big bet,
    /// twice that, on the turn and river. It is read from the hand, so a blind schedule raises the bets with
    /// the blinds, and a big blind posted short by a small stack does not lower it.
    /// </summary>
    internal static decimal FixedBetSize(HandTable table)
    {
        var smallBet = table.SmallBlind * 2;
        return table.Street.BoardSize() >= Street.Turn.BoardSize() ? smallBet * 2 : smallBet;
    }

    /// <summary>
    /// The full-raise increment of <see cref="MinRaiseAmount"/>: the rise in the standing wager a
    /// No-Limit raise has to make, and so the rise that reopens the betting in <see cref="CanRaise"/>.
    /// </summary>
    internal static decimal FullRaiseIncrement(HandTable table) => StandingAndIncrement(table, int.MaxValue).Increment;

    /// <summary>
    /// The largest legal "raise to" total for a seat. No-Limit lets a seat raise its whole stack;
    /// Pot-Limit caps the raise at the pot after the seat calls, and then at the stack; Fixed-Limit
    /// allows exactly one bet over the standing wager, and then no more than the stack.
    /// </summary>
    internal static decimal MaxRaiseAmount(HandTable table, byte seatNo)
    {
        var seat = table[seatNo];
        var allInTo = seat.Stack + seat.StreetWager;
        return table.BettingStructure switch
        {
            BettingStructure.PotLimit => Math.Min(PotLimitRaiseTo(table, seat), allInTo),
            BettingStructure.FixedLimit => Math.Min(MinRaiseAmount(table), allInTo),
            _ => allInTo,
        };
    }

    /// <summary>
    /// The Pot-Limit maximum before the stack cap: the standing wager plus the pot as it would be once
    /// the seat has called. The pot is every chip committed this hand, blinds, dead money and folded
    /// seats' wagers included, which is what <see cref="TableSeat.Committed"/> sums across the seats.
    /// No rounding: amounts have at most two decimal places, and so does their sum.
    /// </summary>
    private static decimal PotLimitRaiseTo(HandTable table, TableSeat seat)
    {
        var agreeAmount = AgreeAmount(table);
        var pot = table.Seats.Sum(other => other.Committed);
        return agreeAmount + pot + (agreeAmount - seat.StreetWager);
    }

    /// <summary>
    /// Whether a seat may raise, an all-in raise included. A seat that has taken no voluntary action
    /// this street may (a blind post is not one, so the big blind keeps its option). A seat that has
    /// may raise only once the standing wager has risen by at least <paramref name="reopenThreshold"/>
    /// since its last voluntary action: an all-in for less than a full raise does not reopen the
    /// betting to a seat that has already acted (TDA Rule 47). The rise is counted cumulatively, so
    /// two short all-ins that together make a full raise do reopen it.
    /// </summary>
    internal static bool CanRaise(HandTable table, byte seatNo, decimal reopenThreshold)
    {
        var lastVoluntary = table[seatNo].StreetBets
            .Where(bet => bet.Action is not (HandActionType.SmallBlind or HandActionType.BigBlind))
            .Select(bet => (int?)bet.Order)
            .LastOrDefault();
        if (lastVoluntary is not { } order)
        {
            return true;
        }
        var standingThen = StandingAndIncrement(table, order).Standing;
        var standingNow = StandingAndIncrement(table, int.MaxValue).Standing;
        return standingNow - standingThen >= reopenThreshold;
    }

    /// <summary>
    /// Whether the seat on the clock may raise at all, an all-in raise included: the betting has to be
    /// open to it (<see cref="CanRaise"/>), and under Fixed-Limit the street must not yet hold
    /// <see cref="HandTable.RaiseCap"/> bets. A No-Limit or Pot-Limit raise reopens the betting when it is
    /// a full raise; a Fixed-Limit one when it is at least half a bet, the limit reading of TDA Rule 47.
    /// </summary>
    internal static bool IsRaisingOpen(HandTable table, byte seatNo)
    {
        if (table.BettingStructure != BettingStructure.FixedLimit)
        {
            return CanRaise(table, seatNo, FullRaiseIncrement(table));
        }
        return CanRaise(table, seatNo, FixedBetSize(table) / 2)
            && (table.RaiseCap is not { } cap || FixedLimitBetCount(table) < cap);
    }

    /// <summary>
    /// The bets this street holds, counted against the Fixed-Limit cap. Pre-flop the big blind is the
    /// first, whatever was actually posted. Every bet and raise counts, and so does an all-in raise that
    /// lifts the standing wager by at least half a bet; a smaller all-in raise does not.
    /// </summary>
    private static int FixedLimitBetCount(HandTable table)
    {
        var halfBet = FixedBetSize(table) / 2;
        var preFlop = table.Street == Street.PreFlop;
        var standing = preFlop ? table.SmallBlind * 2 : 0M;
        var count = preFlop ? 1 : 0;
        foreach (var bet in table.Seats.SelectMany(seat => seat.StreetBets).OrderBy(bet => bet.Order))
        {
            if (!IsRaiseKind(bet) || bet.Action is (HandActionType.SmallBlind or HandActionType.BigBlind) || bet.Amount <= standing)
            {
                continue;
            }
            // Only an all-in can rise by less than a bet, whether it was requested as AllIn or as the
            // offered all-in Raise, so the size alone tells a full bet from a short one.
            if (bet.Amount - standing >= halfBet)
            {
                count++;
            }
            standing = bet.Amount;
        }
        return count;
    }

    // Replays this street's raise-kind wagers in order, up to and including the bet at maxOrder.
    private static (decimal Standing, decimal Increment) StandingAndIncrement(HandTable table, int maxOrder)
    {
        var bigBlind = table.SmallBlind * 2;
        var standing = 0M;
        var increment = bigBlind;
        foreach (var bet in table.Seats.SelectMany(seat => seat.StreetBets).Where(bet => bet.Order <= maxOrder).OrderBy(bet => bet.Order))
        {
            if (!IsRaiseKind(bet) || bet.Amount <= standing)
            {
                continue;
            }
            if (bet.Action is not (HandActionType.SmallBlind or HandActionType.BigBlind) && bet.Amount - standing >= increment)
            {
                increment = bet.Amount - standing;
            }
            standing = bet.Amount;
        }
        return (table.Street == Street.PreFlop ? Math.Max(standing, bigBlind) : standing, increment);
    }

    // An all-in call is recorded as AllIn with AddAmount 0; only an all-in raise counts.
    private static bool IsRaiseKind(SeatBet bet)
        => Array.IndexOf(_raiseKinds, bet.Action) >= 0 || (bet.Action == HandActionType.AllIn && bet.AddAmount > 0);

    internal static bool IsAllIn(TableSeat seat, PlayerActionType action, decimal betAmount)
        => action switch
        {
            // A call that would exceed the stack is an all-in call for less; a raise is only
            // all-in when it lands exactly on the seat's remaining chips.
            PlayerActionType.Call => betAmount >= seat.Stack + seat.StreetWager,
            PlayerActionType.Raise => betAmount == seat.Stack + seat.StreetWager,
            _ => false
        };

    /// <summary>
    /// Caps an all-in at the stack. An action the rules cannot honor is a defect in the caller, not
    /// something to correct: <c>PlayActionHandler</c> validates every action first, so nothing is ever
    /// turned into a fold.
    /// </summary>
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
            throw new InvalidOperationException(
                "PlayActionHandler validates every action before it reaches HandRunner, so an illegal one here is a defect.");
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
            PlayerActionType.Raise => IsShortOfMoney(seat, amount) || MinRaiseAmount(table) > amount
                || amount > MaxRaiseAmount(table, seatNo)
                || !IsRaisingOpen(table, seatNo),
            _ => false
        };
    }

    private static bool IsShortOfMoney(TableSeat seat, decimal amount) => seat.Stack < amount - seat.StreetWager;

    internal static ActionEvaluations LegalActions(HandTable table, byte seatNo)
    {
        var seat = table[seatNo];
        var minRaise = MinRaiseAmount(table);
        return seat.Stack + seat.StreetWager < minRaise
            ? new ActionEvaluations(AgreeAmount(table), null, null)
            : new ActionEvaluations(AgreeAmount(table), minRaise, MaxRaiseAmount(table, seatNo));
    }

    /// <summary>
    /// The actions a seat may take now, exactly as <see cref="HandData.Evaluations"/> offers them, and
    /// the list a <c>Play</c> request is validated against: one source of truth, so a caller choosing
    /// from what it was offered is never rejected, and anything else is.
    /// </summary>
    /// <remarks>
    /// <see cref="PlayerActionEvaluationData.MinValue"/> and <see cref="PlayerActionEvaluationData.MaxValue"/>
    /// on a <see cref="PlayerActionType.Raise"/> are both "raise to" totals, the same absolute basis
    /// as <see cref="HoldemActionPlay.Size"/>, while on a <see cref="PlayerActionType.Call"/> they are
    /// increments — see AGENTS.md.
    /// </remarks>
    internal static IReadOnlyList<PlayerActionEvaluationData> Evaluate(HandTable table, byte seatNo)
    {
        var window = LegalActions(table, seatNo);
        var seat = table[seatNo];

        var evaluations = new List<PlayerActionEvaluationData> { new(PlayerActionType.Fold) };

        var callValue = window.CallTo - seat.StreetWager;
        evaluations.Add(MoreThanZero(callValue)
            ? new PlayerActionEvaluationData(PlayerActionType.Call, callValue, callValue)
            : new PlayerActionEvaluationData(PlayerActionType.Check));

        // The betting is closed to this seat, or the Fixed-Limit cap is reached: no raise window and no
        // all-in raise either.
        if (!IsRaisingOpen(table, seatNo))
        {
            return evaluations;
        }

        if (window.MinRaiseTo is { } minRaiseTo && window.MaxRaiseTo is { } maxRaiseTo)
        {
            var minRaiseIncrement = minRaiseTo - seat.StreetWager;
            if (MoreThanZero(minRaiseIncrement) && MoreThanZero(maxRaiseTo) && maxRaiseTo >= minRaiseTo)
            {
                evaluations.Add(new PlayerActionEvaluationData(PlayerActionType.Raise, minRaiseTo, maxRaiseTo));
                return evaluations;
            }
        }

        // All-in is allowed wherever raising is, even when the raise window above is degenerate. The
        // bound is a "raise to" total like every other raise bound, so it has to include what the seat
        // already has on the street — otherwise a seat that has posted a blind would be handed a size the
        // rules read as an under-raise. It is never offered above the Pot-Limit maximum, and under
        // Fixed-Limit it is offered only when the stack falls short of the one raise-to total.
        var allInRaiseTo = seat.Stack + seat.StreetWager;
        if (allInRaiseTo > MaxRaiseAmount(table, seatNo))
        {
            return evaluations;
        }
        evaluations.Add(new PlayerActionEvaluationData(PlayerActionType.Raise, allInRaiseTo, allInRaiseTo));
        return evaluations;
    }

    private static bool MoreThanZero(decimal amount) => amount > 0.001M;

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
                if (!IsRaiseKind(bet))
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
