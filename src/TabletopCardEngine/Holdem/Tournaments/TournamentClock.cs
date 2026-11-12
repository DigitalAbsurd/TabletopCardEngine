using TabletopCardEngine.Holdem.ActionHandlers;
using TabletopCardEngine.Holdem.Data;

namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>
/// The level clock of a tournament: the level is a function of the time since the start, and nothing else. The
/// levels follow each other with no break, and the last one lasts until the tournament ends, whatever its duration.
/// A hand keeps the level it was dealt at, so this is read when a hand is dealt and when the tournament is read back.
/// </summary>
internal static class TournamentClock
{
    /// <summary>
    /// The index of the level running at <paramref name="now"/>: the first level whose end, counted from
    /// <paramref name="startedAt"/>, is after <paramref name="now"/>, or the last level once every end has passed.
    /// </summary>
    internal static int LevelIndex(IReadOnlyList<TournamentLevel> levels, DateTimeOffset startedAt, DateTimeOffset now)
    {
        var end = startedAt;
        for (var index = 0; index < levels.Count - 1; index++)
        {
            end += TimeSpan.FromSeconds(levels[index].DurationSeconds);
            if (end > now)
            {
                return index;
            }
        }

        return levels.Count - 1;
    }

    /// <summary>When the level at <paramref name="levelIndex"/> ends; <see langword="null"/> for the last level, which never does.</summary>
    internal static DateTimeOffset? LevelEndsAt(IReadOnlyList<TournamentLevel> levels, DateTimeOffset startedAt, int levelIndex)
        => levelIndex >= levels.Count - 1
            ? null
            : startedAt + TimeSpan.FromSeconds(levels.Take(levelIndex + 1).Sum(level => (long)level.DurationSeconds));

    /// <summary>The blinds and ante a table's hand is dealt at on level <paramref name="levelIndex"/>; an ante of <c>0</c> is none.</summary>
    internal static DealLevel DealLevelAt(IReadOnlyList<TournamentLevel> levels, int levelIndex)
    {
        var level = levels[levelIndex];
        return new DealLevel(levelIndex, level.SmallBlind, level.Ante > 0 ? level.Ante : null);
    }
}
