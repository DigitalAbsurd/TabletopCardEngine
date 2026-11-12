namespace TabletopCardEngine.Holdem.Engine;

/// <summary>
/// Side-pot construction and prize splitting, ported from NPokerEngine's <c>GameEvaluator</c>.
/// </summary>
internal static class PotDistribution
{
    private readonly record struct Pot(decimal Amount, IReadOnlyList<TableSeat> Eligible);

    /// <summary>Splits every pot among its eligible winners and returns each seat's total prize.</summary>
    internal static IReadOnlyDictionary<byte, decimal> Distribute(HandTable table)
    {
        var prizes = table.Seats.ToDictionary(seat => seat.SeatNo, _ => 0M);

        foreach (var pot in BuildPots(table.Seats, table.AnteType == AnteType.BigBlind))
        {
            var winners = FindWinners(pot.Eligible, (IReadOnlyList<Card>)table.Board);
            if (winners.Count == 0)
            {
                continue;
            }

            // Split evenly to the cent, and hand the last winner whatever is left over — the
            // smallest chip a pot that doesn't divide evenly can't split further. Rounding each
            // share matters, not just style: an unrounded pot/N repeats forever in decimal for
            // most N, so it can need most of decimal's ~28-29 significant digits, and combining
            // that with a stack of a few thousand chips forces decimal to round the fraction away
            // — differently for different seats, since they rarely have the same number of digits
            // in front of the decimal point. Rounding to the cent up front keeps every downstream
            // amount small enough that no such rounding ever happens.
            var remaining = pot.Amount;
            for (var i = 0; i < winners.Count; i++)
            {
                var share = i == winners.Count - 1 ? remaining : Math.Round(pot.Amount / winners.Count, 2, MidpointRounding.ToEven);
                remaining -= share;
                prizes[winners[i].SeatNo] += share;
            }
        }

        return prizes;
    }

    /// <summary>The best hand(s) among the given (non-folded) seats, evaluated against the board.</summary>
    internal static List<TableSeat> FindWinners(IReadOnlyList<TableSeat> seats, IReadOnlyList<Card> board)
    {
        var active = seats.Where(seat => seat.IsActive).ToList();
        if (active.Count == 0)
        {
            return [];
        }

        var scores = active.ToDictionary(seat => seat, seat => HandEvaluator.EvaluateStrength(seat.Hole, (IReadOnlyList<Card>)board));
        var best = scores.Values.Max();
        return [.. active.Where(seat => scores[seat] == best)];
    }

    /// <summary>
    /// One side pot per all-in level (ascending), then the main pot on top. A big blind ante is dead money for the
    /// lowest pot: it sets no level and goes to whoever wins that pot, so the seat that posted it neither has to be
    /// matched for it nor can win it uncontested (the TDA rule; WSOP hand <c>2023/43/5/03-02-41</c> is the case where a
    /// big blind all in with its ante on top lost to a caller who matched only its wager). A classic ante stays in the
    /// levels: every seat dealt in posted one, so a seat short on its own ante wins only what it matched of the others'.
    /// </summary>
    private static List<Pot> BuildPots(IReadOnlyList<TableSeat> seats, bool bigBlindAnte)
    {
        var levels = seats.ToDictionary(seat => seat, seat => bigBlindAnte ? seat.Committed - seat.Ante : seat.Committed);
        var deadMoney = bigBlindAnte ? seats.Sum(seat => seat.Ante) : 0M;

        var allInLevels = seats
            .Where(seat => seat.Status == SeatStatus.AllIn)
            .Select(seat => levels[seat])
            .OrderBy(level => level)
            .ToList();

        var pots = new List<Pot>();
        foreach (var level in allInLevels)
        {
            pots.Add(CreateSidePot(seats, levels, pots, level));
        }

        pots.Add(CreateMainPot(seats, levels, pots));
        pots[0] = pots[0] with { Amount = pots[0].Amount + deadMoney };
        return pots;
    }

    private static Pot CreateSidePot(IReadOnlyList<TableSeat> seats, Dictionary<TableSeat, decimal> levels, List<Pot> smallerPots,
        decimal allInLevel)
    {
        var amount = seats.Sum(seat => Math.Min(allInLevel, levels[seat])) - smallerPots.Sum(pot => pot.Amount);
        var eligible = seats.Where(seat => levels[seat] >= allInLevel && seat.Status != SeatStatus.Folded).ToList();
        return new Pot(amount, eligible);
    }

    private static Pot CreateMainPot(IReadOnlyList<TableSeat> seats, Dictionary<TableSeat, decimal> levels, List<Pot> sidePots)
    {
        // Measured over the seats still in. In turn, a seat never folds with more in than a seat still playing, so
        // this is the plain maximum; a seat removed mid-hand can fold out of turn with the most in, and its chips are
        // then dead money for the deepest seat still contesting, not a pot nobody can win.
        var contesting = seats.Where(seat => seat.IsActive).ToList();
        var maxLevel = (contesting.Count > 0 ? contesting : seats).Max(seat => levels[seat]);
        var amount = seats.Sum(seat => levels[seat]) - sidePots.Sum(pot => pot.Amount);
        var eligible = seats.Where(seat => levels[seat] == maxLevel).ToList();
        return new Pot(amount, eligible);
    }
}
