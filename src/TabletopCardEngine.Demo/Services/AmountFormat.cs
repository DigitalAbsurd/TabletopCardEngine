using System.Globalization;

namespace TabletopCardEngine.Demo.Services;

/// <summary>
/// The one way the demo writes a chip amount for the visitor. Stacks, pots and bets are whole
/// numbers in almost every game, so a fixed two decimals only printed <c>.00</c> after each of them.
/// Decimals appear only when an amount actually has them: <c>100</c>, <c>12.5</c>, <c>0.25</c>.
/// </summary>
public static class AmountFormat
{
    /// <summary>
    /// Formats an amount with no decimals when it is whole and at most two otherwise. The result is
    /// invariant (a dot for the separator), so it is also valid for an input's value, min or max
    /// attribute, which the browser parses only with a dot.
    /// </summary>
    public static string Format(decimal amount) => amount.ToString("0.##", CultureInfo.InvariantCulture);
}
