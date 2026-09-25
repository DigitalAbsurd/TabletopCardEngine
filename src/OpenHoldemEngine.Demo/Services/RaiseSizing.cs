using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Services;

/// <summary>A reference point on the raise slider: a "raise to" amount and what to call it.</summary>
/// <param name="Amount">The absolute street total the mark sizes the raise to.</param>
/// <param name="Label">The caption shown under the slider.</param>
public sealed record RaiseMark(decimal Amount, string Label);

/// <summary>
/// The reference amounts of the raise slider: the minimum raise, half-pot, pot and all-in, all as
/// absolute "raise to" street totals, the basis a Raise is sent with (fact 5).
/// </summary>
/// <remarks>
/// A pot-sized raise is the call plus the pot as it will stand after that call, on top of the
/// current bet. The seat's own street wager is needed for that, and <see cref="SeatData.BetSize"/>
/// is never populated (fact 4), so it is read off the raise window instead: its top is always the
/// all-in, which is the stack plus whatever the seat has already put in this street.
/// </remarks>
public static class RaiseSizing
{
    /// <summary>The slider distance between two neighbouring marks.</summary>
    public const decimal SegmentLength = 100m;

    /// <summary>How close to a mark, in slider units, a position has to be to take the mark's amount.</summary>
    public const decimal SnapDistance = 2m;

    /// <summary>The half-pot raise, clamped to the legal window; <c>0</c> when no raise is offered.</summary>
    /// <param name="hand">The hand whose seat to act is raising.</param>
    /// <returns>The absolute street total.</returns>
    public static decimal HalfPotRaise(HandData hand) => PotFractionRaise(hand, 0.5m);

    /// <summary>The pot-sized raise, clamped to the legal window; <c>0</c> when no raise is offered.</summary>
    /// <param name="hand">The hand whose seat to act is raising.</param>
    /// <returns>The absolute street total.</returns>
    public static decimal PotRaise(HandData hand) => PotFractionRaise(hand, 1m);

    /// <summary>
    /// Min, ½ Pot, Pot and All-in, in ascending order. ½ Pot or Pot is dropped when clamping puts it
    /// on or past a neighbour — a short stack's window, or a half-pot below the minimum raise — so
    /// every mark is its own point and every segment of the slider has length. Min and All-in are
    /// the ends of the window and always stay.
    /// </summary>
    /// <param name="hand">The current hand, or <see langword="null"/> before the first one.</param>
    /// <returns>The marks; empty when no raise is offered or the window is a single all-in amount.</returns>
    public static IReadOnlyList<RaiseMark> Marks(HandData? hand)
    {
        if (hand is null || RaiseWindow(hand) is not (decimal min, decimal max) || min >= max)
        {
            return [];
        }

        var marks = new List<RaiseMark> { new(min, "Min") };
        foreach (var candidate in new[] { new RaiseMark(HalfPotRaise(hand), "½ Pot"), new RaiseMark(PotRaise(hand), "Pot") })
        {
            if (candidate.Amount > marks[^1].Amount && candidate.Amount < max)
            {
                marks.Add(candidate);
            }
        }

        marks.Add(new RaiseMark(max, "All-in"));
        return marks;
    }

    /// <summary>
    /// Where <paramref name="amount"/> sits on the slider. The marks are spaced evenly, one
    /// <see cref="SegmentLength"/> apart, and the amount is interpolated linearly between the two
    /// around it. On a linear scale a 100-big-blind stack puts Min, ½ Pot and Pot within a couple of
    /// pixels of each other at the left end, and their captions on top of each other.
    /// </summary>
    /// <param name="marks">The marks from <see cref="Marks"/>, ascending, at least two.</param>
    /// <param name="amount">The "raise to" amount.</param>
    /// <returns>The slider position, from <c>0</c> to <see cref="SliderMax"/> of <paramref name="marks"/>.</returns>
    public static double ToPosition(IReadOnlyList<RaiseMark> marks, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(marks);
        if (marks.Count < 2)
        {
            return 0;
        }

        amount = Math.Clamp(amount, marks[0].Amount, marks[^1].Amount);
        var segment = 0;
        while (segment < marks.Count - 2 && amount > marks[segment + 1].Amount)
        {
            segment++;
        }

        var (low, high) = (marks[segment].Amount, marks[segment + 1].Amount);
        var fraction = (amount - low) / (high - low);
        return (double)((segment + fraction) * SegmentLength);
    }

    /// <summary>
    /// The amount at a slider position: the inverse of <see cref="ToPosition"/>, rounded to whole
    /// chips when both ends of the window are whole and to the cent otherwise, so dragging never
    /// offers an amount like 1234.5678. A position within <see cref="SnapDistance"/> of a mark is
    /// that mark's exact amount: a press on a mark's caption reaches the slider too, which reports
    /// the position under the pointer, and that has to land on the mark rather than next to it.
    /// </summary>
    /// <param name="marks">The marks from <see cref="Marks"/>, ascending, at least two.</param>
    /// <param name="position">The slider position.</param>
    /// <returns>The "raise to" amount, inside the window.</returns>
    public static decimal FromPosition(IReadOnlyList<RaiseMark> marks, double position)
    {
        ArgumentNullException.ThrowIfNull(marks);
        if (marks.Count < 2)
        {
            return marks.Count == 1 ? marks[0].Amount : 0;
        }

        var clamped = Math.Clamp((decimal)position, 0, SliderMax(marks));
        for (var i = 0; i < marks.Count; i++)
        {
            if (Math.Abs(clamped - (i * SegmentLength)) <= SnapDistance)
            {
                return marks[i].Amount;
            }
        }

        var scaled = clamped / SegmentLength;
        var segment = Math.Min((int)scaled, marks.Count - 2);
        var (low, high) = (marks[segment].Amount, marks[segment + 1].Amount);
        var amount = low + ((scaled - segment) * (high - low));

        var decimals = marks[0].Amount % 1 == 0 && marks[^1].Amount % 1 == 0 ? 0 : 2;
        return Math.Clamp(Math.Round(amount, decimals, MidpointRounding.AwayFromZero), marks[0].Amount, marks[^1].Amount);
    }

    /// <summary>The right end of the slider's scale for <paramref name="marks"/>.</summary>
    /// <param name="marks">The marks from <see cref="Marks"/>.</param>
    /// <returns><c>0</c> for fewer than two marks.</returns>
    public static decimal SliderMax(IReadOnlyList<RaiseMark> marks)
    {
        ArgumentNullException.ThrowIfNull(marks);
        return Math.Max(0, marks.Count - 1) * SegmentLength;
    }

    private static decimal PotFractionRaise(HandData hand, decimal fraction)
    {
        if (RaiseWindow(hand) is not (decimal min, decimal max))
        {
            return 0;
        }

        var stack = hand.Seats.FirstOrDefault(s => s.SeatNo == hand.NextPlayerSeatNo)?.Stack ?? 0;
        var wager = Math.Max(0, max - stack);
        var call = hand.Evaluations.FirstOrDefault(e => e.PlayerActionType == PlayerActionType.Call)?.MinValue ?? 0;
        var currentBet = wager + call;

        // Pot excludes the current street, so the pot on the table is both of them (fact 6).
        var pot = hand.Pot + hand.StreetPot;
        var raiseTo = Math.Round(currentBet + (fraction * (pot + call)), 2, MidpointRounding.AwayFromZero);
        return Math.Clamp(raiseTo, min, max);
    }

    private static (decimal Min, decimal Max)? RaiseWindow(HandData hand)
    {
        var raise = hand.Evaluations.FirstOrDefault(e => e.PlayerActionType == PlayerActionType.Raise);
        return raise is null ? null : (raise.MinValue, raise.MaxValue);
    }
}
