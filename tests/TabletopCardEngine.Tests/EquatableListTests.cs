using FluentAssertions;
using TabletopCardEngine.Holdem.Mappers;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class EquatableListTests
{
    [TestMethod]
    public void EquatableList_SameElements_AreEqual()
    {
        var first = new EquatableList<Card>([Card.Parse("ah"), Card.Parse("kd")]);
        var second = new EquatableList<Card>([Card.Parse("ah"), Card.Parse("kd")]);

        first.Equals(second).Should().BeTrue();
        first.Equals((object)second).Should().BeTrue();
        EqualityComparer<IReadOnlyList<Card>>.Default.Equals(first, second).Should().BeTrue();
    }

    [TestMethod]
    public void EquatableList_BothEmpty_AreEqual()
    {
        new EquatableList<int>([]).Equals(EquatableList<int>.Empty).Should().BeTrue();
    }

    [TestMethod]
    public void EquatableList_UnequalLength_AreNotEqual()
    {
        var shorter = new EquatableList<int>([1, 2]);
        var longer = new EquatableList<int>([1, 2, 3]);

        shorter.Equals(longer).Should().BeFalse();
        longer.Equals(shorter).Should().BeFalse();
    }

    [TestMethod]
    public void EquatableList_UnequalElement_AreNotEqual()
    {
        var first = new EquatableList<int>([1, 2, 3]);
        var second = new EquatableList<int>([1, 5, 3]);

        first.Equals(second).Should().BeFalse();
    }

    [TestMethod]
    public void EquatableList_SameElementsInOtherOrder_AreNotEqual()
    {
        new EquatableList<int>([1, 2]).Equals(new EquatableList<int>([2, 1])).Should().BeFalse();
    }

    [TestMethod]
    public void EquatableList_Null_IsNotEqual()
    {
        var list = new EquatableList<int>([1]);
        EquatableList<int>? missingList = null;
        object? missingObject = null;

        list.Equals(missingList).Should().BeFalse();
        list.Equals(missingObject).Should().BeFalse();
    }

    [TestMethod]
    public void EquatableList_OtherListType_IsNotEqual()
    {
        var list = new EquatableList<int>([1, 2]);
        object array = new[] { 1, 2 };

        list.Equals(array).Should().BeFalse();
    }

    [TestMethod]
    public void EquatableList_EqualLists_HaveEqualHashCodes()
    {
        var first = new EquatableList<Card>([Card.Parse("ah"), Card.Parse("kd")]);
        var second = new EquatableList<Card>([Card.Parse("ah"), Card.Parse("kd")]);

        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [TestMethod]
    public void EquatableList_DifferentElements_HaveDifferentHashCodes()
    {
        new EquatableList<int>([1, 2, 3]).GetHashCode().Should().NotBe(new EquatableList<int>([1, 2, 4]).GetHashCode());
    }

    [TestMethod]
    public void EquatableList_SourceChangedAfterConstruction_IsNotAffected()
    {
        int[] source = [1, 2, 3];
        var list = new EquatableList<int>(source);

        source[0] = 9;

        list.Should().Equal(1, 2, 3);
        list.Count.Should().Be(3);
        list[0].Should().Be(1);
    }
}
