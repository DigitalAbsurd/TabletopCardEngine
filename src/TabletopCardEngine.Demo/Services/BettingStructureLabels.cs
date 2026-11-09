using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Demo.Services;

/// <summary>
/// Maps <see cref="BettingStructure"/> enum values to their display labels.
/// </summary>
public static class BettingStructureLabels
{
    /// <summary>
    /// Gets the display label for the given betting structure.
    /// </summary>
    /// <param name="structure">The betting structure to get the label for.</param>
    /// <returns>The display label.</returns>
    public static string GetLabel(BettingStructure structure) =>
        structure switch
        {
            BettingStructure.NoLimit => "No-Limit",
            BettingStructure.PotLimit => "Pot-Limit",
            BettingStructure.FixedLimit => "Fixed-Limit",
            _ => structure.ToString(),
        };

    /// <summary>
    /// Gets the label with the raise cap a Fixed-Limit game plays under, such as <c>Fixed-Limit (cap 4)</c>.
    /// The other structures have no cap and read the same as <see cref="GetLabel(BettingStructure)"/>.
    /// </summary>
    /// <param name="structure">The betting structure to get the label for.</param>
    /// <param name="raiseCap">The game's raise cap; <c>null</c> means no cap.</param>
    /// <returns>The display label.</returns>
    public static string GetLabel(BettingStructure structure, int? raiseCap) =>
        structure == BettingStructure.FixedLimit
            ? $"{GetLabel(structure)} ({(raiseCap is { } cap ? $"cap {cap}" : "no cap")})"
            : GetLabel(structure);
}
