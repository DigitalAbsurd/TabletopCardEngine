using FluentAssertions;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Demo.Tests;

[TestClass]
public sealed class RaiseSizing_Tests
{
    private const byte ActorSeatNo = 3;

    // Each case: the pots, the actor's stack, the call it faces (0 for none), the raise window, and
    // the ½ Pot and Pot "raise to" totals. The first is the six-handed opening decision at 50/100
    // that the old formula sent straight to All-in by adding the whole stack to the pot.
    [TestMethod]
    [DataRow(0, 150, 10000, 100, 200, 10000, 225, 350, DisplayName = "PreFlop_Opening")]
    [DataRow(0, 450, 900, 200, 500, 1000, 625, 950, DisplayName = "PreFlop_BigBlindFacingARaise")]
    [DataRow(400, 0, 800, 0, 100, 800, 200, 400, DisplayName = "Flop_FirstToAct")]
    [DataRow(400, 150, 1000, 150, 300, 1000, 500, 850, DisplayName = "Flop_FacingABet")]
    [DataRow(400, 0, 250, 0, 100, 250, 200, 250, DisplayName = "Flop_PotClampedToTheStack")]
    public void PotRaises_AreRaiseToTotals(
        int pot, int streetPot, int stack, int call, int min, int max, int expectedHalfPot, int expectedPot)
    {
        // Arrange
        var hand = Hand(pot, streetPot, stack, call, min, max);

        // Act and Assert
        RaiseSizing.HalfPotRaise(hand).Should().Be(expectedHalfPot);
        RaiseSizing.PotRaise(hand).Should().Be(expectedPot);
    }

    [TestMethod]
    public void Marks_PreFlopOpening_AreMinHalfPotPotAllIn()
    {
        // Act
        var marks = RaiseSizing.Marks(Hand(0, 150, 10000, 100, 200, 10000));

        // Assert - ½ Pot and Pot sit strictly inside the window
        marks.Should().Equal(
            new RaiseMark(200, "Min"),
            new RaiseMark(225, "½ Pot"),
            new RaiseMark(350, "Pot"),
            new RaiseMark(10000, "All-in"));
    }

    [TestMethod]
    public void Marks_NarrowWindow_CollapseToItsEnds()
    {
        // Arrange - 250 behind on a 400 pot: both pot raises clamp onto the ends of 100..250
        var hand = Hand(400, 0, 250, 0, 100, 250);

        // Act
        var marks = RaiseSizing.Marks(hand);

        // Assert - ½ Pot (200) survives, Pot clamps onto All-in and is dropped
        marks.Select(mark => mark.Label).Should().Equal("Min", "½ Pot", "All-in");
        // A half-pot of 150 clamps onto the 200 minimum, which is the Min mark itself
        RaiseSizing.Marks(Hand(0, 150, 1000, 50, 200, 1000)).Select(mark => mark.Label)
            .Should().Equal("Min", "Pot", "All-in");
    }

    [TestMethod]
    public void Marks_NoRaiseOrAllInOnly_AreEmpty()
    {
        RaiseSizing.Marks(null).Should().BeEmpty();
        RaiseSizing.Marks(Hand(0, 150, 150, 100, 150, 150)).Should().BeEmpty();
        RaiseSizing.Marks(Hand(0, 150, 1000, 100, 0, 0) with { Evaluations = [new(PlayerActionType.Fold)] })
            .Should().BeEmpty();
    }

    [TestMethod]
    public void Positions_SpaceTheMarksEvenlyAndRoundTrip()
    {
        // Arrange
        var marks = RaiseSizing.Marks(Hand(0, 150, 10000, 100, 200, 10000));

        // Act and Assert - one segment per pair of marks, whatever the amounts between them
        RaiseSizing.SliderMax(marks).Should().Be(300);
        marks.Select(mark => RaiseSizing.ToPosition(marks, mark.Amount)).Should().Equal(0d, 100d, 200d, 300d);
        marks.Select(mark => RaiseSizing.FromPosition(marks, RaiseSizing.ToPosition(marks, mark.Amount)))
            .Should().Equal(200m, 225m, 350m, 10000m);

        // Halfway along the last segment, rounded to whole chips because the window is whole
        RaiseSizing.FromPosition(marks, 250).Should().Be(5175m);
        RaiseSizing.FromPosition(marks, 205).Should().Be(833m);

        // Near a mark is the mark: the slider reports where a press on a caption went down
        RaiseSizing.FromPosition(marks, 201.5).Should().Be(350m);
        RaiseSizing.FromPosition(marks, 298.6).Should().Be(10000m);

        // Out of range clamps to the window
        RaiseSizing.FromPosition(marks, -5).Should().Be(200m);
        RaiseSizing.ToPosition(marks, 20000m).Should().Be(300d);
    }

    private static HandData Hand(int pot, int streetPot, int stack, int call, int min, int max)
    {
        var evaluations = new List<PlayerActionEvaluationData> { new(PlayerActionType.Fold) };
        evaluations.Add(call > 0 ? new(PlayerActionType.Call, call, call) : new(PlayerActionType.Check));
        if (max > 0)
        {
            evaluations.Add(new(PlayerActionType.Raise, min, max));
        }

        return new HandData(
            No: 1,
            Seats: [new SeatData(ActorSeatNo, "Actor", stack, stack)],
            DealerSeatNo: 1,
            SmallBlindSeatNo: 1,
            BigBlindSeatNo: 2,
            NextPlayerSeatNo: ActorSeatNo,
            Street: pot == 0 ? Street.PreFlop : Street.Flop,
            StreetPot: streetPot,
            Pot: pot,
            CommunityCards: [],
            Evaluations: evaluations.ToArray());
    }
}
