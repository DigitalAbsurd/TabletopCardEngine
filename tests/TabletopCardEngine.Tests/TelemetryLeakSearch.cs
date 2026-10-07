using System.Text.RegularExpressions;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Finds dealt cards in telemetry text. A card is matched as a whole token (<c>ah</c> in
/// <c>seat 3 holds ah, kd</c>, but not in <c>shadow</c> or inside a GUID), because a search on the
/// two letters alone would fire on every GUID and action name in the corpus and make the gate useless.
/// </summary>
internal static class TelemetryLeakSearch
{
    /// <summary>Builds the whole-token, case-insensitive pattern for one card (it covers the PHH spelling too).</summary>
    internal static Regex PatternFor(Card card)
        => new($"(?<![0-9a-z]){Regex.Escape(card.ToString())}(?![0-9a-z])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Returns the cards of <paramref name="cards"/> that occur in any of the texts.</summary>
    internal static IReadOnlyList<Card> FindCards(IEnumerable<string> texts, IEnumerable<Card> cards)
    {
        var materialized = texts.ToList();
        return [.. cards.Where(card =>
        {
            var pattern = PatternFor(card);
            return materialized.Any(pattern.IsMatch);
        })];
    }
}
