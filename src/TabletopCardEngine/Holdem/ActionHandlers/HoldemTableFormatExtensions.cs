namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <summary>
/// The one place that knows how many seats each <see cref="HoldemTableFormat"/> allows. The format
/// caps the seats; <c>MaxPlayersCount</c> stays the number of seats at the table.
/// </summary>
internal static class HoldemTableFormatExtensions
{
    /// <summary>Fewest seats a table of this format may have.</summary>
    internal static int MinSeats(this HoldemTableFormat format) => format switch
    {
        HoldemTableFormat.HeadsUp => 2,
        HoldemTableFormat.SixMax => 2,
        HoldemTableFormat.FullRing => 7,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
    };

    /// <summary>Most seats a table of this format may have.</summary>
    internal static int MaxSeats(this HoldemTableFormat format) => format switch
    {
        HoldemTableFormat.HeadsUp => 2,
        HoldemTableFormat.SixMax => 6,
        HoldemTableFormat.FullRing => 10,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
    };
}
