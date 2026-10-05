using FluentAssertions;
using OpenHoldemEngine.Demo.Services;
using System.Globalization;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
public sealed class AmountFormat_Tests
{
    // A decimal is not a constant attribute argument, so each amount is written as a string and
    // parsed. Parsing "100.00" also keeps its scale, which is exactly the case that used to print
    // trailing zeros.
    [TestMethod]
    [DataRow("100", "100")]
    [DataRow("100.00", "100")]
    [DataRow("0", "0")]
    [DataRow("12.5", "12.5")]
    [DataRow("12.50", "12.5")]
    [DataRow("0.25", "0.25")]
    [DataRow("-2", "-2")]
    [DataRow("-0.75", "-0.75")]
    [DataRow("1234567", "1234567")]
    public void Format_ShowsDecimalsOnlyWhenTheAmountHasThem(string amount, string expected)
    {
        // Arrange
        var value = decimal.Parse(amount, CultureInfo.InvariantCulture);

        // Act
        var formatted = AmountFormat.Format(value);

        // Assert
        formatted.Should().Be(expected);
    }

    [TestMethod]
    public void Format_IsInvariantWhateverTheCurrentCulture()
    {
        // Arrange - a culture with a comma for the decimal separator
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            // Act
            var formatted = AmountFormat.Format(12.5m);

            // Assert - an input's value attribute is parsed with a dot only
            formatted.Should().Be("12.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
