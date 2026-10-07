namespace TabletopCardEngine.Engine;

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

        foreach (var pot in BuildPots(table.Seats))
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

    /// <summary>One side pot per all-in level (ascending), then the main pot on top.</summary>
    private static List<Pot> BuildPots(IReadOnlyList<TableSeat> seats)
    {
        var allInLevels = seats
            .Where(seat => seat.Status == SeatStatus.AllIn)
            .Select(seat => seat.Committed)
            .OrderBy(committed => committed)
            .ToList();

        var sidePots = new List<Pot>();
        foreach (var level in allInLevels)
        {
            sidePots.Add(CreateSidePot(seats, sidePots, level));
        }

        sidePots.Add(CreateMainPot(seats, sidePots));
        return sidePots;
    }

    private static Pot CreateSidePot(IReadOnlyList<TableSeat> seats, List<Pot> smallerPots, decimal allInLevel)
    {
        var amount = seats.Sum(seat => Math.Min(allInLevel, seat.Committed)) - smallerPots.Sum(pot => pot.Amount);
        var eligible = seats.Where(seat => seat.Committed >= allInLevel && seat.Status != SeatStatus.Folded).ToList();
        return new Pot(amount, eligible);
    }

    private static Pot CreateMainPot(IReadOnlyList<TableSeat> seats, List<Pot> sidePots)
    {
        var maxCommitted = seats.Max(seat => seat.Committed);
        var amount = seats.Sum(seat => seat.Committed) - sidePots.Sum(pot => pot.Amount);
        var eligible = seats.Where(seat => seat.Committed == maxCommitted).ToList();
        return new Pot(amount, eligible);
    }
}
