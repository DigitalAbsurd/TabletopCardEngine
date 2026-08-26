using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Engine;

/// <summary>
/// Runs the betting rules over a <see cref="HandTable"/>: starting a hand and applying one player
/// action at a time. Ported from NPokerEngine's <c>Emulator</c> + <c>RoundManager</c>. The table
/// is mutated in place — callers persist whatever parts of it they care about and discard the
/// rest, the same way it was rehydrated fresh from the database on the way in.
/// </summary>
internal static class HandRunner
{
    /// <summary>
    /// Starts a new hand: shifts the button, collects blinds from whoever can afford them (folding
    /// anyone who cannot), deals hole cards, and advances to the first decision — or straight to
    /// showdown, if the blinds alone leave one player standing.
    /// </summary>
    /// <exception cref="InvalidOperationException">Fewer than two seats can cover the blinds.</exception>
    internal static void StartHand(HandTable table)
    {
        ShiftButton(table);
        ExcludeShortOfMoneySeats(table);

        if (table.ActiveCount < 2)
        {
            throw new InvalidOperationException("At least two funded seats are required to start a hand.");
        }

        PostBlinds(table);
        DealHoleCards(table);
        StartStreet(table);
    }

    /// <summary>
    /// Applies one action for the seat currently on the clock, advances the street (or ends the
    /// hand, possibly running the board all the way out first) as needed, and reports what happened.
    /// </summary>
    internal static AppliedAction ApplyAction(HandTable table, PlayerActionType requestedAction, decimal requestedAmount)
    {
        var seatNo = table.NextSeatNo;
        var seat = table[seatNo];
        var agreeAmountBeforeAction = BettingRules.AgreeAmount(table);

        var (action, amount) = BettingRules.CorrectAction(table, seatNo, Normalize(requestedAction), requestedAmount);
        if (BettingRules.IsAllIn(seat, action, amount))
        {
            seat.Status = SeatStatus.AllIn;
        }

        var (recordedAmount, addAmount, paid) = Accept(seat, action, amount, agreeAmountBeforeAction);
        var street = table.Street;

        table.Prizes = null;
        IReadOnlyList<Card> board;
        if (EveryoneAgreed(table))
        {
            foreach (var s in table.Seats)
            {
                s.ClearStreet();
            }
            table.Street = table.Street.Next();
            board = StartStreet(table);
        }
        else
        {
            table.NextSeatNo = table.NextSeatAfter(seatNo, s => s.IsWaiting);
            board = [];
        }

        return new AppliedAction(seatNo, action, recordedAmount, addAmount, paid,
            street, table.Street, table.NextSeatNo, board, table.Prizes);
    }

    /// <summary>
    /// The betting rules only distinguish Fold/Call/Raise (plus the blinds, posted separately).
    /// A check is a call for zero more chips; a bet, a raise, and an all-in raise are all "bring
    /// the street total up to this amount" — the caller's intent, not the rules' vocabulary.
    /// </summary>
    private static PlayerActionType Normalize(PlayerActionType action) => action switch
    {
        PlayerActionType.Check => PlayerActionType.Call,
        PlayerActionType.Bet or PlayerActionType.AllIn => PlayerActionType.Raise,
        _ => action
    };

    private static (decimal Amount, decimal AddAmount, decimal Paid) Accept(
        TableSeat seat, PlayerActionType action, decimal amount, decimal agreeAmountBeforeAction)
    {
        switch (action)
        {
            case PlayerActionType.Call:
                {
                    var paid = CollectDelta(seat, amount);
                    seat.Record(PlayerActionType.Call, amount, 0M, paid);
                    return (amount, 0M, paid);
                }
            case PlayerActionType.Raise:
                {
                    var addAmount = amount - agreeAmountBeforeAction;
                    var paid = CollectDelta(seat, amount);
                    seat.Record(PlayerActionType.Raise, amount, addAmount, paid);
                    return (amount, addAmount, paid);
                }
            case PlayerActionType.Fold:
                {
                    seat.Record(PlayerActionType.Fold, 0M, 0M, 0M);
                    seat.Status = SeatStatus.Folded;
                    return (0M, 0M, 0M);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unexpected action.");
        }
    }

    private static decimal CollectDelta(TableSeat seat, decimal streetTotal)
    {
        var delta = streetTotal - seat.StreetWager;
        seat.Stack -= delta;
        seat.Committed += delta;
        return delta;
    }

    private static void ShiftButton(HandTable table)
        => table.DealerSeatNo = table.NextSeatAfter(table.DealerSeatNo, s => s.IsActive && s.Stack != 0);

    /// <summary>
    /// Zeroes out anyone who cannot cover the small blind (or, failing that, the big blind),
    /// folds every broke seat, and settles who actually posts. Ported from NPokerEngine's
    /// <c>ExcludeShortOfMoneyPlayers</c> / <c>SteelMoneyFromPoorPlayer</c> — there is no ante here,
    /// so "cannot cover the ante" never applies.
    /// </summary>
    private static void ExcludeShortOfMoneySeats(HandTable table)
    {
        var (smallBlindSeatNo, bigBlindSeatNo) = StealFromPoorSeats(table);
        table.SmallBlindSeatNo = smallBlindSeatNo;
        table.BigBlindSeatNo = bigBlindSeatNo;

        foreach (var seat in table.Seats.Where(seat => seat.Stack <= 0))
        {
            seat.Status = SeatStatus.Folded;
        }
    }

    private static (byte SmallBlindSeatNo, byte BigBlindSeatNo) StealFromPoorSeats(HandTable table)
    {
        foreach (var seat in table.Seats.Where(seat => seat.Stack <= 0))
        {
            seat.Stack = 0;
        }
        if (table[table.DealerSeatNo].Stack <= 0)
        {
            ShiftButton(table);
        }

        var ordered = table.Seats.OrderBy(seat => seat.SeatNo).ToList();
        var dealerIndex = ordered.FindIndex(seat => seat.SeatNo == table.DealerSeatNo);
        var ring = Enumerable.Range(0, ordered.Count * 3).Select(index => ordered[index % ordered.Count]).ToList();
        var searchTargets = ring.Skip(dealerIndex + 1).Take(ordered.Count).ToList();

        var sbSeat = FindFirstEligible(searchTargets, table.SmallBlind);
        if (sbSeat is null)
        {
            // Nobody can post anything; StartHand's ActiveCount < 2 check reports the failure.
            return (0, 0);
        }
        foreach (var seat in searchTargets.Take(searchTargets.IndexOf(sbSeat)))
        {
            seat.Stack = 0;
        }

        var afterSb = searchTargets.Skip(searchTargets.IndexOf(sbSeat) + 1).Take(ordered.Count - 1).ToList();
        var bbSeat = FindFirstEligible(afterSb, table.SmallBlind * 2) ?? sbSeat;

        if (bbSeat == sbSeat)
        {
            // Nobody can cover the big blind either — steal from everyone except the small blind.
            foreach (var seat in table.Seats.Where(seat => seat != sbSeat))
            {
                seat.Stack = 0;
            }
        }
        else
        {
            foreach (var seat in afterSb.Take(afterSb.IndexOf(bbSeat)))
            {
                seat.Stack = 0;
            }
        }

        return (sbSeat.SeatNo, bbSeat.SeatNo);
    }

    private static TableSeat? FindFirstEligible(IEnumerable<TableSeat> seats, decimal needAmount)
        => seats.FirstOrDefault(seat => seat.Stack >= needAmount);

    private static void PostBlinds(HandTable table)
    {
        // Heads-up, the button posts the small blind and acts last preflop but first postflop.
        if (table.ActiveCount == 2)
        {
            (table.SmallBlindSeatNo, table.BigBlindSeatNo) = (table.BigBlindSeatNo, table.SmallBlindSeatNo);
        }

        PostBlind(table[table.SmallBlindSeatNo], PlayerActionType.SmallBlind, table.SmallBlind, table.SmallBlind);
        PostBlind(table[table.BigBlindSeatNo], PlayerActionType.BigBlind, table.SmallBlind * 2, table.SmallBlind);
    }

    private static void PostBlind(TableSeat seat, PlayerActionType kind, decimal amount, decimal addAmount)
    {
        seat.Stack -= amount;
        seat.Committed += amount;
        seat.Record(kind, amount, addAmount, paid: amount);
    }

    private static void DealHoleCards(HandTable table)
    {
        foreach (var seat in table.Seats.OrderBy(seat => seat.SeatNo))
        {
            seat.Hole.Clear();
            seat.Hole.AddRange(table.Deck.Draw(2));
        }
    }

    private static IReadOnlyList<Card> StartStreet(HandTable table)
    {
        // Only one seat left in the hand: no more cards need to be dealt, win it outright. Without
        // this, a hand decided by folds — the common case — would run the board out anyway (as
        // NPokerEngine does internally) and end up persisting board cards the history never dealt.
        if (table.ActiveCount <= 1)
        {
            return Resolve(table);
        }

        table.NextSeatNo = table.NextSeatFrom(table.SmallBlindSeatNo, s => s.IsWaiting);
        return table.Street switch
        {
            Street.PreFlop => PreFlop(table),
            Street.Flop => DealStreet(table, 3),
            Street.Turn => DealStreet(table, 1),
            Street.River => DealStreet(table, 1),
            Street.Showdown => Resolve(table),
            _ => throw new InvalidOperationException($"Street {table.Street} cannot be started.")
        };
    }

    private static IReadOnlyList<Card> PreFlop(HandTable table)
    {
        // Skip past the small and big blind to under-the-gun.
        for (var i = 0; i < 2; i++)
        {
            table.NextSeatNo = table.NextSeatAfter(table.NextSeatNo, s => s.IsWaiting);
        }
        return ForwardStreet(table);
    }

    private static IReadOnlyList<Card> DealStreet(HandTable table, int count)
    {
        var dealt = table.Deck.Draw(count);
        table.Board.AddRange(dealt);

        // The button acts first postflop when heads-up.
        if (table.DealerSeatNo == table.SmallBlindSeatNo && table.NextSeatNo != 0)
        {
            table.NextSeatNo = table.NextSeatAfter(table.NextSeatNo, s => s.IsWaiting);
        }

        return [.. dealt, .. ForwardStreet(table)];
    }

    /// <summary>
    /// When at most one seat can still be asked (everyone else is all in or folded), keeps
    /// dealing streets and advancing without stopping to ask anyone, all the way to showdown.
    /// </summary>
    private static IReadOnlyList<Card> ForwardStreet(HandTable table)
    {
        if (table.WaitingCount <= 1)
        {
            table.Street = table.Street.Next();
            return StartStreet(table);
        }
        return [];
    }

    private static IReadOnlyList<Card> Resolve(HandTable table)
    {
        var prizes = PotDistribution.Distribute(table);
        foreach (var (seatNo, amount) in prizes)
        {
            table[seatNo].Stack += amount;
        }
        table.Prizes = prizes;
        table.Street = Street.Finished;
        table.NextSeatNo = 0;
        return [];
    }

    private static bool EveryoneAgreed(HandTable table)
    {
        if (table.ActiveCount == 0)
        {
            throw new InvalidOperationException("No active seats remain.");
        }

        var maxWager = table.Seats.Max(seat => seat.StreetWager);
        var everyoneAgreed = table.Seats.All(s => IsAgreed(table, s, maxWager));
        var lonelySeat = table.ActiveCount == 1;

        var nextWaitingSeatNo = table.NextSeatAfter(table.NextSeatNo, s => s.IsWaiting);
        var nextSeat = nextWaitingSeatNo == 0 ? null : table[nextWaitingSeatNo];
        var noNeedToAsk = table.WaitingCount == 1 && nextSeat is not null && nextSeat.StreetWager == maxWager;

        return everyoneAgreed || lonelySeat || noNeedToAsk;
    }

    private static bool IsAgreed(HandTable table, TableSeat seat, decimal maxWager)
    {
        // The big blind gets an option even if nobody has raised over its own post.
        var isPreFlop = table.Street == Street.PreFlop;
        var bigBlindAskCheck = !isPreFlop || !seat.BigBlindOptionPending;
        return (bigBlindAskCheck && seat.StreetWager == maxWager && seat.HasActedThisStreet)
            || seat.Status is SeatStatus.Folded or SeatStatus.AllIn;
    }
}
