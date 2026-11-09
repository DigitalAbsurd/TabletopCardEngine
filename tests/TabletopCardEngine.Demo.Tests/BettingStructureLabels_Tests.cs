using FluentAssertions;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Demo.Tests;

[TestClass]
public sealed class BettingStructureLabels_Tests
{
    [TestMethod]
    [DataRow(BettingStructure.NoLimit, "No-Limit")]
    [DataRow(BettingStructure.PotLimit, "Pot-Limit")]
    [DataRow(BettingStructure.FixedLimit, "Fixed-Limit")]
    public void GetLabel_NamesEveryStructure(BettingStructure structure, string expected)
    {
        BettingStructureLabels.GetLabel(structure).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(BettingStructure.NoLimit, 4, "No-Limit")]
    [DataRow(BettingStructure.PotLimit, 4, "Pot-Limit")]
    [DataRow(BettingStructure.FixedLimit, 4, "Fixed-Limit (cap 4)")]
    [DataRow(BettingStructure.FixedLimit, 3, "Fixed-Limit (cap 3)")]
    [DataRow(BettingStructure.FixedLimit, null, "Fixed-Limit (no cap)")]
    public void GetLabel_WithRaiseCap_ShowsTheCapOnlyForFixedLimit(BettingStructure structure, int? raiseCap, string expected)
    {
        BettingStructureLabels.GetLabel(structure, raiseCap).Should().Be(expected);
    }
}
