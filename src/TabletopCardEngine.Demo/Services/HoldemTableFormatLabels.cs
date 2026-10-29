using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Demo.Services;

/// <summary>
/// Maps <see cref="HoldemTableFormat"/> enum values to their display labels.
/// </summary>
public static class HoldemTableFormatLabels
{
    /// <summary>
    /// Gets the display label for the given table format.
    /// </summary>
    /// <param name="format">The table format to get the label for.</param>
    /// <returns>The display label.</returns>
    public static string GetLabel(HoldemTableFormat format) =>
        format switch
        {
            HoldemTableFormat.HeadsUp => "Heads-Up (2-Max)",
            HoldemTableFormat.SixMax => "6-Max",
            HoldemTableFormat.FullRing => "Full Ring",
            _ => format.ToString(),
        };
}
