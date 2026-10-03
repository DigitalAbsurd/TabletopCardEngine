using FluentAssertions;
using Microsoft.Extensions.Primitives;
using OpenHoldemEngine.AspNetCore.Http;

namespace OpenHoldemEngine.AspNetCore.Tests;

/// <summary>
/// Tests for <see cref="HoldemETag"/>, which translates GameData.Version to ETag and If-Match headers.
/// </summary>
[TestClass]
public sealed class HoldemETagTests
{
    private static readonly string[] _multipleHeaderValuesArray = new[] { "\"7\"", "\"8\"" };

    [TestMethod]
    [DataRow(1, "\"1\"")]
    public void Format_ReturnsQuotedString(int version, string expected)
    {
        var actual = HoldemETag.Format(version);
        actual.Should().Be(expected);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(42)]
    [DataRow(int.MaxValue)]
    public void Format_RoundTripsWithParseIfMatch(int version)
    {
        var formatted = HoldemETag.Format(version);
        var parsed = HoldemETag.ParseIfMatch(new StringValues(formatted));

        parsed.Present.Should().BeTrue();
        parsed.Valid.Should().BeTrue();
        parsed.ExpectedVersion.Should().Be(version);
    }

    [TestMethod]
    public void ParseIfMatch_Absent_ReturnsNotPresentAndValid()
    {
        var actual = HoldemETag.ParseIfMatch(StringValues.Empty);

        actual.Present.Should().BeFalse();
        actual.Valid.Should().BeTrue();
        actual.ExpectedVersion.Should().BeNull();
    }

    [TestMethod]
    public void ParseIfMatch_Wildcard_ReturnsPresentValidButNoVersion()
    {
        var actual = HoldemETag.ParseIfMatch(new StringValues("*"));

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeTrue();
        actual.ExpectedVersion.Should().BeNull();
    }

    [TestMethod]
    public void ParseIfMatch_ValidStrongTag_ReturnsPresentValidWithVersion()
    {
        var actual = HoldemETag.ParseIfMatch(new StringValues("\"7\""));

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeTrue();
        actual.ExpectedVersion.Should().Be(7);
    }

    [TestMethod]
    public void ParseIfMatch_WeakTag_ReturnsPresentButInvalid()
    {
        var actual = HoldemETag.ParseIfMatch(new StringValues("W/\"7\""));

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeFalse();
        actual.ExpectedVersion.Should().BeNull();
    }

    [TestMethod]
    public void ParseIfMatch_UnquotedValue_ReturnsPresentButInvalid()
    {
        var actual = HoldemETag.ParseIfMatch(new StringValues("7"));

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeFalse();
        actual.ExpectedVersion.Should().BeNull();
    }

    [TestMethod]
    public void ParseIfMatch_CommaSeparatedList_ReturnsPresentButInvalid()
    {
        var actual = HoldemETag.ParseIfMatch(new StringValues("\"7\", \"8\""));

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeFalse();
        actual.ExpectedVersion.Should().BeNull();
    }

    [TestMethod]
    public void ParseIfMatch_MultipleHeaderValues_ReturnsPresentButInvalid()
    {
        var values = new StringValues(_multipleHeaderValuesArray);
        var actual = HoldemETag.ParseIfMatch(values);

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeFalse();
        actual.ExpectedVersion.Should().BeNull();
    }

    [TestMethod]
    public void ParseIfMatch_NonIntegerTag_ReturnsPresentButInvalid()
    {
        var actual = HoldemETag.ParseIfMatch(new StringValues("\"abc\""));

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeFalse();
        actual.ExpectedVersion.Should().BeNull();
    }

    [TestMethod]
    public void ParseIfMatch_ZeroVersion_ReturnsPresentButInvalid()
    {
        var actual = HoldemETag.ParseIfMatch(new StringValues("\"0\""));

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeFalse();
        actual.ExpectedVersion.Should().BeNull();
    }

    [TestMethod]
    public void ParseIfMatch_NegativeVersion_ReturnsPresentButInvalid()
    {
        var actual = HoldemETag.ParseIfMatch(new StringValues("\"-1\""));

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeFalse();
        actual.ExpectedVersion.Should().BeNull();
    }

    [TestMethod]
    public void ParseIfMatch_OutOfIntRangeVersion_ReturnsPresentButInvalid()
    {
        var actual = HoldemETag.ParseIfMatch(new StringValues("\"99999999999\""));

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeFalse();
        actual.ExpectedVersion.Should().BeNull();
    }

    [TestMethod]
    public void ParseIfMatch_WhitespaceOnly_ReturnsPresentButInvalid()
    {
        var actual = HoldemETag.ParseIfMatch(new StringValues(" "));

        actual.Present.Should().BeTrue();
        actual.Valid.Should().BeFalse();
        actual.ExpectedVersion.Should().BeNull();
    }
}
