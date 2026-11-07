using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Holdem.Engine;

/// <summary>
/// The blinds a hand is dealt at and the bookkeeping the next hand needs to work out its own, exactly the four
/// columns a <c>Hand</c> row stores.
/// </summary>
/// <param name="SmallBlind">The nominal small blind; the big blind is always twice it.</param>
/// <param name="BlindLevel">How many levels the blinds have risen by; <c>0</c> is the initial level.</param>
/// <param name="BlindLevelStartHandNo">The hand at which <paramref name="BlindLevel"/> began, which the every-N-hands trigger counts from.</param>
/// <param name="BlindThresholdsPassed">How many of the schedule's player thresholds, sorted descending, have already fired.</param>
internal readonly record struct BlindLevelState(decimal SmallBlind, int BlindLevel, int BlindLevelStartHandNo, int BlindThresholdsPassed);

/// <summary>
/// Works out the blinds of a new hand from the previous hand's stored state. The engine loads only the latest
/// hand, so everything a trigger needs is carried from hand to hand in <see cref="BlindLevelState"/> rather than
/// derived from the hand number or the history.
/// <para>
/// Three triggers can fire for hand <c>n &gt; 1</c>: <see cref="BlindScheduleData.IncreaseEveryHands"/> hands at
/// the current level, the players dealt in falling to or below a threshold not yet passed, and fewer players
/// dealt in than into the previous hand. However many fire, the level rises by exactly one, and it never falls.
/// Hand 1 is dealt at the initial level and counts every threshold it already meets as passed, so a table that
/// starts short-handed does not raise on its second hand for a threshold it never crossed.
/// </para>
/// </summary>
internal static class BlindSchedule
{
    /// <summary>Stacks are decimal, so a seat "has chips" when its stack reaches this, never when it is above zero.</summary>
    internal const decimal DecimalMinPrecision = 0.0001M;

    /// <summary>Returns the state hand <paramref name="handNo"/> is dealt with.</summary>
    /// <param name="schedule">The game's schedule; <see langword="null"/> deals every hand at the initial level.</param>
    /// <param name="initialSmallBlind">The game's initial small blind, the blind of level <c>0</c>.</param>
    /// <param name="previous">The state hand <c><paramref name="handNo"/> - 1</c> was dealt with; ignored for hand 1.</param>
    /// <param name="previousDealtSeats">The seats dealt into the previous hand; ignored for hand 1.</param>
    /// <param name="handNo">The number of the hand being dealt.</param>
    /// <param name="dealtSeats">The seats being dealt into this hand: those starting it with chips.</param>
    internal static BlindLevelState Next(BlindScheduleData? schedule, decimal initialSmallBlind, BlindLevelState previous,
        int previousDealtSeats, int handNo, int dealtSeats)
    {
        if (schedule is null)
        {
            return new BlindLevelState(initialSmallBlind, 0, handNo, 0);
        }

        var thresholds = schedule.PlayerThresholds.OrderDescending().ToList();
        if (handNo <= 1)
        {
            return new BlindLevelState(initialSmallBlind, 0, handNo, PassThresholds(thresholds, 0, dealtSeats));
        }

        var thresholdsPassed = PassThresholds(thresholds, previous.BlindThresholdsPassed, dealtSeats);
        var raise = handNo - previous.BlindLevelStartHandNo >= schedule.IncreaseEveryHands
            || thresholdsPassed > previous.BlindThresholdsPassed
            || (schedule.IncreaseOnElimination && dealtSeats < previousDealtSeats);

        if (!raise)
        {
            return previous with { BlindThresholdsPassed = thresholdsPassed };
        }

        var level = previous.BlindLevel + 1;
        return new BlindLevelState(RaisedSmallBlind(schedule, previous.SmallBlind, level), level, handNo, thresholdsPassed);
    }

    /// <summary>Walks the descending thresholds on from <paramref name="passed"/> while each is met; returns the new count.</summary>
    private static int PassThresholds(List<int> thresholdsDescending, int passed, int dealtSeats)
    {
        while (passed < thresholdsDescending.Count && thresholdsDescending[passed] >= dealtSeats)
        {
            passed++;
        }
        return passed;
    }

    private static decimal RaisedSmallBlind(BlindScheduleData schedule, decimal previousSmallBlind, int level)
    {
        if (schedule.Mode == BlindIncreaseMode.Levels)
        {
            var levels = schedule.SmallBlindLevels;
            return levels[Math.Min(level, levels.Count) - 1];
        }

        var raised = Math.Round(previousSmallBlind * schedule.Multiplier, 2, MidpointRounding.AwayFromZero);
        return schedule.MaxSmallBlind is { } cap ? Math.Min(raised, cap) : raised;
    }
}
