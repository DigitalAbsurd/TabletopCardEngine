using System.Globalization;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Demo.Services;

/// <summary>A reference point of a raise: a "raise to" amount and what to call it.</summary>
/// <param name="Amount">The absolute street total the mark sizes the raise to.</param>
/// <param name="Label">The caption shown for it.</param>
/// <param name="Formula">
/// How the amount was worked out, for a pot preset; <see langword="null"/> for Min and All-in, which
/// are the engine's own numbers.
/// </param>
public sealed record RaiseMark(decimal Amount, string Label, string? Formula = null);

/// <summary>What the raise amount box holds: an amount the engine will accept, or why it will not.</summary>
/// <param name="Amount">The "raise to" amount, or <see langword="null"/> when the text is not a legal raise.</param>
/// <param name="Error">Why the text is not a legal raise; <see langword="null"/> when it is.</param>
public readonly record struct RaiseInput(decimal? Amount, string? Error)
{
    /// <summary>Whether the text is a raise the engine will accept.</summary>
    public bool IsValid => Error is null;
}

/// <summary>
/// The reference amounts of a raise: the minimum raise and all-in at the ends of the slider, and
/// the half-pot and pot presets beside it, all as absolute "raise to" street totals, the basis a
/// Raise is sent with (fact 5). One step of the amount is the small blind, the smallest chip on the
/// table, so every amount the visitor is offered is a multiple of it — except Min and All-in
/// themselves, which are whatever the engine says they are.
/// </summary>
/// <remarks>
/// A pot-sized raise is the call plus the pot as it will stand after that call, on top of the
/// current bet. The seat's own street wager is needed for that: <see cref="SeatData.BetSize"/>.
/// <br/>That is why Pot is bigger than the pot on the table: at 1/2, first to act pre-flop with 3 in
/// the pot, Pot is 2 + (3 + 2) = 7. Each preset carries that sum as its
/// <see cref="RaiseMark.Formula"/>, so the visitor can see where the number comes from.
/// </remarks>
public static class RaiseSizing
{
    /// <summary>The slider distance between two neighbouring marks.</summary>
    public const decimal SegmentLength = 100m;

    /// <summary>How close to a mark, in slider units, a position has to be to take the mark's amount.</summary>
    public const decimal SnapDistance = 2m;

    /// <summary>The half-pot raise, clamped to the legal window; <c>0</c> when no raise is offered.</summary>
    /// <param name="hand">The hand whose seat to act is raising.</param>
    /// <param name="step">The amount's step, the small blind; <c>0</c> rounds to the cent instead.</param>
    /// <returns>The absolute street total.</returns>
    public static decimal HalfPotRaise(HandData hand, decimal step = 0) => PotFractionRaise(hand, 0.5m, step)?.Amount ?? 0;

    /// <summary>The pot-sized raise, clamped to the legal window; <c>0</c> when no raise is offered.</summary>
    /// <param name="hand">The hand whose seat to act is raising.</param>
    /// <param name="step">The amount's step, the small blind; <c>0</c> rounds to the cent instead.</param>
    /// <returns>The absolute street total.</returns>
    public static decimal PotRaise(HandData hand, decimal step = 0) => PotFractionRaise(hand, 1m, step)?.Amount ?? 0;

    /// <summary>
    /// Min and All-in, the ends of the raise window and the only marks on the slider. ½ Pot and Pot
    /// are buttons of their own (<see cref="Presets"/>): as marks they crowded the slider's left end.
    /// </summary>
    /// <param name="hand">The current hand, or <see langword="null"/> before the first one.</param>
    /// <returns>The marks; empty when no raise is offered or the window is a single all-in amount.</returns>
    public static IReadOnlyList<RaiseMark> Marks(HandData? hand)
    {
        if (hand is null || RaiseWindow(hand) is not (decimal min, decimal max) || min >= max)
        {
            return [];
        }

        return [new RaiseMark(min, "Min"), new RaiseMark(max, "All-in")];
    }

    /// <summary>
    /// ½ Pot and Pot, in ascending order. A preset is dropped when clamping puts it on or past a
    /// neighbour — a short stack's window, or a half-pot below the minimum raise — because a button
    /// that does what Min or the slider's right end already does is only noise. Each preset carries
    /// its sum as <see cref="RaiseMark.Formula"/>.
    /// </summary>
    /// <param name="hand">The current hand, or <see langword="null"/> before the first one.</param>
    /// <param name="step">The amount's step, the small blind.</param>
    /// <returns>The presets; empty when no raise is offered or the window is a single all-in amount.</returns>
    public static IReadOnlyList<RaiseMark> Presets(HandData? hand, decimal step)
    {
        if (hand is null || RaiseWindow(hand) is not (decimal min, decimal max) || min >= max)
        {
            return [];
        }

        var presets = new List<RaiseMark>();
        var floor = min;
        foreach (var (fraction, label) in new[] { (0.5m, "½ Pot"), (1m, "Pot") })
        {
            if (PotFractionRaise(hand, fraction, step) is not PotRaiseSum sum)
            {
                continue;
            }

            var candidate = new RaiseMark(sum.Amount, label, sum.Formula);
            if (candidate.Amount > floor && candidate.Amount < max)
            {
                presets.Add(candidate);
                floor = candidate.Amount;
            }
        }

        return presets;
    }

    /// <summary>
    /// Moves <paramref name="amount"/> to the next multiple of <paramref name="step"/> in
    /// <paramref name="direction"/>, clamped to the window. From an amount that is not a multiple —
    /// an odd minimum raise — the first step lands on the nearest multiple on that side, so the
    /// amount falls into line with the step instead of carrying the odd remainder along.
    /// </summary>
    /// <param name="amount">The current "raise to" amount.</param>
    /// <param name="direction">Positive for up, negative for down.</param>
    /// <param name="step">The amount's step, the small blind.</param>
    /// <param name="min">The minimum raise-to.</param>
    /// <param name="max">The all-in raise-to.</param>
    /// <returns>The new amount, inside the window.</returns>
    public static decimal Step(decimal amount, int direction, decimal step, decimal min, decimal max)
    {
        if (step <= 0 || direction == 0)
        {
            return Math.Clamp(amount, min, max);
        }

        var next = direction > 0
            ? (Math.Floor(amount / step) + 1) * step
            : (Math.Ceiling(amount / step) - 1) * step;
        return Math.Clamp(next, min, max);
    }

    /// <summary>
    /// Reads the amount box. A legal raise is a number inside the window that is a multiple of the
    /// step, or exactly Min or All-in, which need not be. A comma is read as the decimal point, the
    /// way a visitor on a comma-decimal keyboard types it.
    /// </summary>
    /// <param name="text">What the box holds.</param>
    /// <param name="step">The amount's step, the small blind.</param>
    /// <param name="min">The minimum raise-to.</param>
    /// <param name="max">The all-in raise-to.</param>
    /// <returns>The amount, or the reason there is none.</returns>
    public static RaiseInput Validate(string? text, decimal step, decimal min, decimal max)
    {
        if (!TryParseAmount(text, out var amount))
        {
            return new RaiseInput(null, "Enter an amount");
        }

        if (amount < min)
        {
            return new RaiseInput(null, $"The minimum raise is to {AmountFormat.Format(min)}");
        }

        if (amount > max)
        {
            return new RaiseInput(null, $"All-in is {AmountFormat.Format(max)}");
        }

        if (amount != min && amount != max && step > 0 && amount % step != 0)
        {
            return new RaiseInput(null, $"Raise in steps of {AmountFormat.Format(step)}");
        }

        return new RaiseInput(amount, null);
    }

    /// <summary>
    /// The amount the auto-correct button puts in the box: the typed number clamped to the window and
    /// rounded to the nearest step, or <paramref name="lastValid"/> when the box holds no number. The
    /// result always passes <see cref="Validate"/>.
    /// </summary>
    /// <param name="text">What the box holds.</param>
    /// <param name="lastValid">The last legal amount the box held.</param>
    /// <param name="step">The amount's step, the small blind.</param>
    /// <param name="min">The minimum raise-to.</param>
    /// <param name="max">The all-in raise-to.</param>
    /// <returns>A legal "raise to" amount.</returns>
    public static decimal Correct(string? text, decimal lastValid, decimal step, decimal min, decimal max)
    {
        if (!TryParseAmount(text, out var amount))
        {
            return Math.Clamp(lastValid, min, max);
        }

        return Math.Clamp(RoundToStep(Math.Clamp(amount, min, max), step), min, max);
    }

    /// <summary>
    /// Where <paramref name="amount"/> sits on the slider. The marks are spaced evenly, one
    /// <see cref="SegmentLength"/> apart, and the amount is interpolated linearly between the two
    /// around it.
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
    /// The amount at a slider position: the inverse of <see cref="ToPosition"/>, rounded to a
    /// multiple of <paramref name="step"/> — or, without one, to whole chips when both ends of the
    /// window are whole and to the cent otherwise — so dragging never offers an amount like
    /// 1234.5678. A position within <see cref="SnapDistance"/> of a mark is that mark's exact amount:
    /// a press on a mark's caption reaches the slider too, which reports the position under the
    /// pointer, and that has to land on the mark rather than next to it.
    /// </summary>
    /// <param name="marks">The marks from <see cref="Marks"/>, ascending, at least two.</param>
    /// <param name="position">The slider position.</param>
    /// <param name="step">The amount's step, the small blind; <c>0</c> for none.</param>
    /// <returns>The "raise to" amount, inside the window.</returns>
    public static decimal FromPosition(IReadOnlyList<RaiseMark> marks, double position, decimal step = 0)
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

        if (step > 0)
        {
            return Math.Clamp(RoundToStep(amount, step), marks[0].Amount, marks[^1].Amount);
        }

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

    private static PotRaiseSum? PotFractionRaise(HandData hand, decimal fraction, decimal step)
    {
        if (RaiseWindow(hand) is not (decimal min, decimal max))
        {
            return null;
        }

        var wager = hand.Seats.FirstOrDefault(s => s.SeatNo == hand.NextPlayerSeatNo)?.BetSize ?? 0;
        var call = hand.Evaluations.FirstOrDefault(e => e.PlayerActionType == PlayerActionType.Call)?.MinValue ?? 0;
        var currentBet = wager + call;

        // Pot excludes the current street, so the pot on the table is both of them (fact 6).
        var pot = hand.Pot + hand.StreetPot;
        var exact = currentBet + (fraction * (pot + call));
        var rounded = step > 0 ? RoundToStep(exact, step) : Math.Round(exact, 2, MidpointRounding.AwayFromZero);
        var amount = Math.Clamp(rounded, min, max);
        return new PotRaiseSum(amount, Explain(currentBet, fraction, pot, call, exact, rounded, amount));
    }

    /// <summary>
    /// The pot-limit sum written out, e.g. "Current bet 2 + ½ × (pot 3 + call 2) = 4.5, rounded to 5",
    /// followed by whatever the step or the raise window changed about it.
    /// </summary>
    private static string Explain(decimal currentBet, decimal fraction, decimal pot, decimal call, decimal exact, decimal rounded, decimal amount)
    {
        var potTerm = call > 0
            ? $"(pot {AmountFormat.Format(pot)} + call {AmountFormat.Format(call)})"
            : $"pot {AmountFormat.Format(pot)}";
        var formula = $"Current bet {AmountFormat.Format(currentBet)} + {(fraction == 1m ? string.Empty : "½ × ")}{potTerm} = {AmountFormat.Format(exact)}";
        if (AmountFormat.Format(rounded) != AmountFormat.Format(exact))
        {
            formula += $", rounded to {AmountFormat.Format(rounded)}";
        }

        if (amount > rounded)
        {
            formula += $", raised to the minimum {AmountFormat.Format(amount)}";
        }
        else if (amount < rounded)
        {
            formula += $", capped at all-in {AmountFormat.Format(amount)}";
        }

        return formula;
    }

    private static (decimal Min, decimal Max)? RaiseWindow(HandData hand)
    {
        var raise = hand.Evaluations.FirstOrDefault(e => e.PlayerActionType == PlayerActionType.Raise);
        return raise is null ? null : (raise.MinValue, raise.MaxValue);
    }

    private static decimal RoundToStep(decimal amount, decimal step) =>
        step > 0 ? Math.Round(amount / step, MidpointRounding.AwayFromZero) * step : amount;

    private static bool TryParseAmount(string? text, out decimal amount) =>
        decimal.TryParse(
            text?.Trim().Replace(',', '.'),
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out amount);

    /// <summary>A pot preset's amount and the sum that produced it.</summary>
    private readonly record struct PotRaiseSum(decimal Amount, string Formula);
}
