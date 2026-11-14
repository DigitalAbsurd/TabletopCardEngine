using TabletopCardEngine.Holdem.Tournaments;

namespace TabletopCardEngine.Demo.Services;

/// <summary>
/// The clock the demo's engine reads its tournament blind levels from: system time plus an offset the
/// demo can advance, so a visitor does not have to wait out a five-minute level to see the blinds rise.
/// </summary>
public sealed class DemoClock : TimeProvider
{
    private long _offsetTicks;

    /// <summary>Gets how far this clock runs ahead of system time.</summary>
    public TimeSpan Offset => TimeSpan.FromTicks(Interlocked.Read(ref _offsetTicks));

    /// <summary>Moves the clock forward, which ends the running <see cref="TournamentLevelData"/> sooner.</summary>
    /// <param name="amount">How far to move it; it must not be negative, because a clock that runs backwards would bring a level back.</param>
    public void Advance(TimeSpan amount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(amount, TimeSpan.Zero);
        Interlocked.Add(ref _offsetTicks, amount.Ticks);
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + Offset;
}
