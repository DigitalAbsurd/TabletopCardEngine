namespace TabletopCardEngine.Holdem.Engine;

/// <summary>
/// Runs the betting rules over a <see cref="HandTable"/>: starting a hand and applying one player
/// action at a time. Ported from NPokerEngine's <c>Emulator</c> + <c>RoundManager</c>. The table
/// is mutated in place — callers persist whatever parts of it they care about and discard the
/// rest, the same way it was rehydrated fresh from the database on the way in.
/// </summary>
internal static class HandRunner
{
    /// <summary>
    /// Starts a new hand: shifts the button, settles who posts the blinds by position (folding every broke
    /// seat), collects the classic antes, then the blinds — capped at what each seat has left — and then the
    /// big blind ante, deals hole cards, and advances to the first decision, or straight to showdown if the
    /// antes and blinds alone leave one player standing.
    /// </summary>
    /// <exception cref="InvalidOperationException">Fewer than two seats have chips to play with.</exception>
    internal static void StartHand(HandTable table)
    {
        ShiftButton(table);

        // The button and the blinds go by position, as the rules have them: they are settled before any
        // chip goes in, among every seat with chips, so a seat too short for its blind posts what it has,
        // and a seat a classic ante then empties keeps its place and posts a blind of nothing.
        ExcludeShortOfMoneySeats(table);

        // Counted on chips rather than on ActiveCount, because a seat with less than a full blind is
        // a playable hand — it posts what it has and is all in — not a seat that cannot be dealt to.
        if (table.Seats.Count(seat => seat.IsActive && (seat.Stack > 0 || seat.Status == SeatStatus.AllIn)) < 2
            || table.SmallBlindSeatNo == 0 || table.BigBlindSeatNo == 0)
        {
            throw new InvalidOperationException("At least two funded seats are required to start a hand.");
        }

        if (table.Ante > 0 && table.AnteType == AnteType.Classic)
        {
            PostClassicAntes(table);
        }

        PostBlinds(table);
        if (table.Ante > 0 && table.AnteType == AnteType.BigBlind)
        {
            PostAnte(table[table.BigBlindSeatNo], table.Ante);
        }

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

        var (recordedAmount, addAmount, paid) = Accept(table, seat, action, amount, agreeAmountBeforeAction);
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
    /// Folds <paramref name="seatNo"/> whether or not it is on the clock, for a seat removed from the
    /// table mid-hand. On the clock it is exactly the fold <see cref="ApplyAction"/> records. Out of
    /// turn the clock does not move — the seat to act still owes the same decision, and nothing about
    /// the street can close on a seat that has not acted — unless the fold leaves one seat contesting
    /// the pot, which then wins it through the same resolution as any fold-out.
    /// </summary>
    /// <exception cref="InvalidOperationException">The seat is not waiting to act: it has folded or is all in.</exception>
    internal static AppliedAction FoldSeat(HandTable table, byte seatNo)
    {
        if (seatNo == table.NextSeatNo)
        {
            return ApplyAction(table, PlayerActionType.Fold, 0M);
        }

        var seat = table[seatNo];
        if (!seat.IsWaiting)
        {
            throw new InvalidOperationException("Only a seat still owing decisions can be folded.");
        }

        var (recordedAmount, addAmount, paid) = Accept(table, seat, PlayerActionType.Fold, 0M, BettingRules.AgreeAmount(table));
        var street = table.Street;

        table.Prizes = null;
        var board = table.ActiveCount <= 1 ? StartStreet(table) : [];

        return new AppliedAction(seatNo, PlayerActionType.Fold, recordedAmount, addAmount, paid,
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
        HandTable table, TableSeat seat, PlayerActionType action, decimal amount, decimal agreeAmountBeforeAction)
    {
        switch (action)
        {
            case PlayerActionType.Call:
                {
                    var paid = CollectDelta(seat, amount);
                    seat.Record(HandActionType.Call, amount, 0M, paid, table.NextBetOrder++);
                    return (amount, 0M, paid);
                }
            case PlayerActionType.Raise:
                {
                    var addAmount = amount - agreeAmountBeforeAction;
                    var paid = CollectDelta(seat, amount);
                    seat.Record(HandActionType.Raise, amount, addAmount, paid, table.NextBetOrder++);
                    return (amount, addAmount, paid);
                }
            case PlayerActionType.Fold:
                {
                    seat.Record(HandActionType.Fold, 0M, 0M, 0M, table.NextBetOrder++);
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
    /// Folds every broke seat and settles who posts the blinds, by position: the small blind is the first
    /// seat with chips after the button and the big blind the next one, whatever each can cover, and a short
    /// one posts what it has (<see cref="PostBlind"/>). Heads-up the button posts the small blind.
    /// </summary>
    private static void ExcludeShortOfMoneySeats(HandTable table)
    {
        foreach (var seat in table.Seats.Where(seat => seat.Stack <= 0))
        {
            seat.Stack = 0;
            if (seat.Status != SeatStatus.AllIn)
            {
                seat.Status = SeatStatus.Folded;
            }
        }
        if (table[table.DealerSeatNo].Stack <= 0)
        {
            ShiftButton(table);
        }

        if (table.WaitingCount == 2)
        {
            table.SmallBlindSeatNo = table.DealerSeatNo;
            table.BigBlindSeatNo = table.Seats.First(seat => seat.IsWaiting && seat.SeatNo != table.DealerSeatNo).SeatNo;
            return;
        }

        var smallBlindSeatNo = table.NextSeatAfter(table.DealerSeatNo, seat => seat.Stack > 0);
        var bigBlindSeatNo = smallBlindSeatNo == 0 ? (byte)0 : table.NextSeatAfter(smallBlindSeatNo, seat => seat.Stack > 0);

        // One seat with chips cannot be both blinds; StartHand reports that a hand cannot start.
        table.SmallBlindSeatNo = smallBlindSeatNo;
        table.BigBlindSeatNo = bigBlindSeatNo == smallBlindSeatNo ? (byte)0 : bigBlindSeatNo;
    }

    private static void PostBlinds(HandTable table)
    {
        PostBlind(table, table[table.SmallBlindSeatNo], HandActionType.SmallBlind, table.SmallBlind, table.SmallBlind);
        PostBlind(table, table[table.BigBlindSeatNo], HandActionType.BigBlind, table.SmallBlind * 2, table.SmallBlind);
    }

    /// <summary>Every seat dealt into the hand posts the ante, before any blind.</summary>
    private static void PostClassicAntes(HandTable table)
    {
        foreach (var seat in table.Seats.Where(seat => seat.IsActive && seat.Stack > 0))
        {
            PostAnte(seat, table.Ante);
        }
    }

    /// <summary>
    /// Posts an ante, capped at the stack like a blind. It goes into <see cref="TableSeat.Committed"/> only and
    /// is never recorded as a street bet: it is dead money, so it neither counts towards a call nor sets
    /// the minimum raise, and the big blind keeps its option.
    /// </summary>
    private static void PostAnte(TableSeat seat, decimal ante)
    {
        var posted = Math.Min(ante, seat.Stack);
        seat.Stack -= posted;
        seat.Committed += posted;
        seat.Ante += posted;

        if (seat.Stack == 0)
        {
            seat.Status = SeatStatus.AllIn;
        }
    }

    /// <summary>
    /// Posts one blind, capped at the stack: a seat with less than the blind posts what it has and is
    /// all in, which is what the rules say and is common heads-up. The recorded amount is what was
    /// actually wagered, so the pot maths, the side-pot levels and the callers' view of the street
    /// all agree with the stack that was taken.
    /// </summary>
    private static void PostBlind(HandTable table, TableSeat seat, HandActionType kind, decimal amount, decimal addAmount)
    {
        var posted = Math.Min(amount, seat.Stack);
        seat.Stack -= posted;
        seat.Committed += posted;
        seat.Record(kind, posted, Math.Min(addAmount, posted), paid: posted, order: table.NextBetOrder++);

        if (seat.Stack == 0)
        {
            seat.Status = SeatStatus.AllIn;
        }
    }

    private static void DealHoleCards(HandTable table)
    {
        foreach (var seat in table.Seats.OrderBy(seat => seat.SeatNo))
        {
            seat.Hole.Clear();

            // A seat the deal has sat out still draws its two cards, so the deck order and the cards
            // ICardShuffler supplies per seat do not move, but it keeps none: with no hole cards it is
            // rehydrated as out of the hand (HandTableFactory.BuildSeat).
            var cards = table.Deck.Draw(2);
            if (seat.Status != SeatStatus.Folded)
            {
                seat.Hole.AddRange(cards);
            }
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
        // Under-the-gun is the first waiting seat after the big blind. A blind that went all in on its
        // post is not waiting, so counting two waiting seats on from the small blind would overshoot.
        // Heads-up the walk wraps round to the button, which acts first, or to the big blind when the
        // button is all in.
        table.NextSeatNo = table.NextSeatAfter(table.BigBlindSeatNo, s => s.IsWaiting);
        return ForwardStreet(table);
    }

    private static IReadOnlyList<Card> DealStreet(HandTable table, int count)
    {
        var dealt = table.Deck.Draw(count);
        table.Board.AddRange(dealt);

        // Heads-up — which is what the button holding the small blind means — the big blind acts
        // first on every street after the pre-flop, so step one seat past the button.
        if (table.DealerSeatNo == table.SmallBlindSeatNo && table.NextSeatNo != 0)
        {
            table.NextSeatNo = table.NextSeatAfter(table.NextSeatNo, s => s.IsWaiting);
        }

        return [.. dealt, .. ForwardStreet(table)];
    }

    /// <summary>
    /// When nobody is left to ask (everyone else is all in or folded), keeps dealing streets and
    /// advancing without stopping, all the way to showdown.
    /// </summary>
    private static IReadOnlyList<Card> ForwardStreet(HandTable table)
    {
        if (CanCloseWithoutAsking(table))
        {
            table.Street = table.Street.Next();
            return StartStreet(table);
        }
        return [];
    }

    /// <summary>
    /// Whether the street is settled with nobody asked: at most one seat is still waiting, and if
    /// there is one it has already matched the biggest wager. A lone waiting seat that still owes
    /// chips has a real decision left, and heads-up that is not a theoretical case — against a big
    /// blind that went all in for less than a full blind, the button has only its small blind in and
    /// still has to call or fold. Post-flop every street opens with the wagers cleared, so the only
    /// waiting seat trivially matches and the board runs itself out as before.
    /// </summary>
    private static bool CanCloseWithoutAsking(HandTable table)
    {
        if (table.WaitingCount > 1)
        {
            return false;
        }

        var maxWager = table.Seats.Max(seat => seat.StreetWager);
        return table.Seats.Where(seat => seat.IsWaiting).All(seat => seat.StreetWager == maxWager);
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
        // A blind gets an option even if nobody has raised over its own post.
        var isPreFlop = table.Street == Street.PreFlop;
        var blindAskCheck = !isPreFlop || !seat.BlindOptionPending;
        return (blindAskCheck && seat.StreetWager == maxWager && seat.HasActedThisStreet)
            || seat.Status is SeatStatus.Folded or SeatStatus.AllIn;
    }
}
