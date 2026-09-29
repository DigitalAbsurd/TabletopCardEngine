using FluentAssertions;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
public sealed class ObjectDump_Tests
{
    public sealed record Leaf(int Number, string Text);

    public sealed record Level1(Leaf Inner);

    public sealed record Level0(Level1 Middle);

    [TestMethod]
    [DataRow(42, "42", "Int32 (System)")]
    [DataRow(true, "true", "Boolean (System)")]
    [DataRow("abc", "\"abc\"", "String (System)")]
    [DataRow(PlayerActionType.Raise, "Raise", "PlayerActionType (OpenHoldemEngine)")]
    public void Create_Scalar_ShowsTheValueAndHasNoChildren(object value, string expectedValue, string expectedType)
    {
        // Act
        var node = ObjectDump.Create("x", value, typeof(object));

        // Assert
        node.Value.Should().Be(expectedValue);
        node.TypeName.Should().Be(expectedType);
        node.HasChildren.Should().BeFalse();
    }

    [TestMethod]
    public void Create_DecimalAndCard_AreInvariantLeaves()
    {
        ObjectDump.Create("d", 12.5m, typeof(decimal)).Value.Should().Be("12.5");

        var card = ObjectDump.Create("c", Card.FromString("ah"), typeof(Card));
        card.Value.Should().Be("ah");
        card.HasChildren.Should().BeFalse();
    }

    [TestMethod]
    public void Create_Null_ShowsNullWithTheDeclaredType()
    {
        var node = ObjectDump.Create("hand", null, typeof(HandData));

        node.Value.Should().Be("null");
        node.TypeName.Should().Be("HandData (OpenHoldemEngine.Dto)");
        node.HasChildren.Should().BeFalse();
    }

    [TestMethod]
    public void Create_Array_PreviewsItsItemsAndNamesThemByIndex()
    {
        // Act
        var node = ObjectDump.Create("hole", new[] { Card.FromString("ah"), Card.FromString("kd") }, typeof(Card[]));

        // Assert - a value, not "Card[]" or "<enumerable Count: 2>"
        node.Value.Should().Be("[ah, kd]");
        node.TypeName.Should().Be("Card[] (OpenHoldemEngine)");
        node.Children.Select(child => child.Name).Should().Equal("[0]", "[1]");
        node.Children.Select(child => child.Value).Should().Equal("ah", "kd");
    }

    [TestMethod]
    public void Create_Object_PreviewsItsMembersAlphabetically()
    {
        // Act
        var node = ObjectDump.Create("leaf", new Leaf(1, "x"), typeof(Leaf));

        // Assert
        node.Value.Should().Be("{ Number = 1, Text = \"x\" }");
        node.Children.Select(child => child.Name).Should().Equal("Number", "Text");
    }

    [TestMethod]
    public void Create_DeepObject_PreviewStopsTwoLevelsDown()
    {
        // Act
        var node = ObjectDump.Create("root", new Level0(new Level1(new Leaf(1, "x"))), typeof(Level0));

        // Assert - the third level is elided in the preview but still reachable by expanding
        node.Value.Should().Be("{ Middle = { Inner = {…} } }");
        node.Children.Single().Children.Single().Value.Should().Be("{ Number = 1, Text = \"x\" }");
    }

    [TestMethod]
    public void Create_LongList_PreviewIsCapped()
    {
        var node = ObjectDump.Create("numbers", Enumerable.Range(0, 1000).ToArray(), typeof(int[]));

        node.Value.Length.Should().BeLessOrEqualTo(ObjectDump.MaxPreviewLength + 1);
        node.Value.Should().EndWith("…");
    }

    [TestMethod]
    public void FormatTypeName_GenericTypes_AreFriendly()
    {
        ObjectDump.FormatTypeName(typeof(HoldemResult<GameData>)).Should().Be("HoldemResult<GameData> (OpenHoldemEngine)");
        ObjectDump.FormatTypeName(typeof(List<HoldemError>)).Should().Be("List<HoldemError> (System.Collections.Generic)");
        ObjectDump.FormatTypeName(typeof(decimal?)).Should().Be("Decimal? (System)");
    }

    [TestMethod]
    public void Create_FailedResult_ShowsTheThrowingValueGetterAsAValue()
    {
        // Arrange - Result<T>.Value throws on a failed result
        var result = HoldemResult<GameData>.Fail(new HoldemError(HoldemErrorCode.CurrentHandEnded, "The current hand is completed"));

        // Act
        var node = ObjectDump.Create("result", result, typeof(HoldemResult<GameData>));

        // Assert
        var value = node.Children.Single(child => child.Name == "Value");
        value.Value.Should().StartWith("threw InvalidOperationException");
        value.HasChildren.Should().BeFalse();
        node.Children.Single(child => child.Name == "IsFailed").Value.Should().Be("true");
        node.Children.Single(child => child.Name == "Errors").Children.Single()
            .Children.Single(child => child.Name == "Message").Value.Should().Be("\"The current hand is completed\"");
    }

    [TestMethod]
    public void Create_SuccessfulResult_ReachesTheSeatsThroughValue()
    {
        // Arrange
        var seat = new SeatData(SeatNo: 1, Name: "Player 1", StartHandStack: 100m, Stack: 98m, Hole: [Card.FromString("ah"), Card.FromString("kd")]);
        var hand = new HandData(1, [seat], 1, 1, 2, 2, Street.PreFlop, 3m, 0m, [], []);
        var result = HoldemResult<GameData>.Ok(new GameData(Guid.Empty, new GameOptionData(6, 1m, 1), hand));

        // Act
        var node = ObjectDump.Create("result", result, typeof(HoldemResult<GameData>));

        // Assert
        node.TypeName.Should().Be("HoldemResult<GameData> (OpenHoldemEngine)");
        node.Value.Should().StartWith("{ ");
        var seats = node.Children.Single(child => child.Name == "Value")
            .Children.Single(child => child.Name == "HandData")
            .Children.Single(child => child.Name == "Seats");
        seats.TypeName.Should().Be("SeatData[] (OpenHoldemEngine.Dto)");
        var hole = seats.Children.Single().Children.Single(child => child.Name == "Hole");
        hole.Value.Should().Be("[ah, kd]");
        seats.Children.Single().Value.Should().Contain("Name = \"Player 1\"").And.Contain("Hole = [ah, kd]");
    }
}
