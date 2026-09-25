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

    // The same decisions with the presets rounded to the 50 small blind at 50/100: 225 and 625 sit
    // halfway between two steps and round up.
    [TestMethod]
    [DataRow(0, 150, 10000, 100, 200, 10000, 250, 350, DisplayName = "PreFlop_Opening")]
    [DataRow(0, 450, 900, 200, 500, 1000, 650, 950, DisplayName = "PreFlop_BigBlindFacingARaise")]
    [DataRow(400, 150, 1000, 150, 300, 1000, 500, 850, DisplayName = "Flop_FacingABet")]
    public void PotRaises_RoundToTheSmallBlind(
        int pot, int streetPot, int stack, int call, int min, int max, int expectedHalfPot, int expectedPot)
    {
        // Arrange
        var hand = Hand(pot, streetPot, stack, call, min, max);

        // Act and Assert
        RaiseSizing.HalfPotRaise(hand, 50m).Should().Be(expectedHalfPot);
        RaiseSizing.PotRaise(hand, 50m).Should().Be(expectedPot);
    }

    [TestMethod]
    public void Marks_AreOnlyTheEndsOfTheWindow()
    {
        // Act
        var marks = RaiseSizing.Marks(Hand(0, 150, 10000, 100, 200, 10000));

        // Assert - ½ Pot and Pot are presets beside the slider, not marks on it
        marks.Should().Equal(new RaiseMark(200, "Min"), new RaiseMark(10000, "All-in"));
    }

    [TestMethod]
    public void Presets_PreFlopOpening_AreHalfPotAndPot()
    {
        // Act
        var presets = RaiseSizing.Presets(Hand(0, 150, 10000, 100, 200, 10000), 50m);

        // Assert - both strictly inside the window, on the step
        presets.Should().Equal(new RaiseMark(250, "½ Pot"), new RaiseMark(350, "Pot"));
    }

    [TestMethod]
    public void Presets_NarrowWindow_DropWhatLandsOnAnEnd()
    {
        // Arrange - 250 behind on a 400 pot: Pot clamps onto All-in and is dropped, ½ Pot (200) stays
        RaiseSizing.Presets(Hand(400, 0, 250, 0, 100, 250), 50m).Select(preset => preset.Label)
            .Should().Equal("½ Pot");

        // A half-pot of 150 clamps onto the 200 minimum, which Min already offers
        RaiseSizing.Presets(Hand(0, 150, 1000, 50, 200, 1000), 50m).Select(preset => preset.Label)
            .Should().Equal("Pot");
    }

    [TestMethod]
    public void MarksAndPresets_NoRaiseOrAllInOnly_AreEmpty()
    {
        RaiseSizing.Marks(null).Should().BeEmpty();
        RaiseSizing.Marks(Hand(0, 150, 150, 100, 150, 150)).Should().BeEmpty();
        RaiseSizing.Marks(Hand(0, 150, 1000, 100, 0, 0) with { Evaluations = [new(PlayerActionType.Fold)] })
            .Should().BeEmpty();
        RaiseSizing.Presets(null, 50m).Should().BeEmpty();
        RaiseSizing.Presets(Hand(0, 150, 150, 100, 150, 150), 50m).Should().BeEmpty();
    }

    [TestMethod]
    public void Positions_AreLinearBetweenTheEndsAndRoundToTheStep()
    {
        // Arrange
        var marks = RaiseSizing.Marks(Hand(0, 150, 10000, 100, 200, 10000));

        // Act and Assert - one segment, from Min to All-in
        RaiseSizing.SliderMax(marks).Should().Be(100);
        marks.Select(mark => RaiseSizing.ToPosition(marks, mark.Amount)).Should().Equal(0d, 100d);
        RaiseSizing.ToPosition(marks, 5100m).Should().Be(50d);

        // Halfway is 5100; without a step it is whole chips because the window is whole
        RaiseSizing.FromPosition(marks, 50).Should().Be(5100m);
        RaiseSizing.FromPosition(marks, 50.3).Should().Be(5129m);

        // With a step it lands on a multiple of the small blind
        RaiseSizing.FromPosition(marks, 50.3, 50m).Should().Be(5150m);
        RaiseSizing.FromPosition(marks, 10.1, 50m).Should().Be(1200m);

        // Near an end is the end itself, even when the end is off the step
        var odd = RaiseSizing.Marks(Hand(0, 150, 987, 100, 200, 987));
        RaiseSizing.FromPosition(odd, 99, 50m).Should().Be(987m);
        RaiseSizing.FromPosition(odd, 96, 50m).Should().Be(950m);

        // Out of range clamps to the window
        RaiseSizing.FromPosition(marks, -5, 50m).Should().Be(200m);
        RaiseSizing.ToPosition(marks, 20000m).Should().Be(100d);
    }

    [TestMethod]
    [DataRow(200, 1, 250, DisplayName = "Up_FromAMultiple")]
    [DataRow(250, -1, 200, DisplayName = "Down_FromAMultiple")]
    [DataRow(230, 1, 250, DisplayName = "Up_FromOffTheStep_LandsOnTheNextMultiple")]
    [DataRow(230, -1, 200, DisplayName = "Down_FromOffTheStep_LandsOnThePreviousMultiple")]
    [DataRow(200, -1, 200, DisplayName = "Down_AtMin_Clamps")]
    [DataRow(975, 1, 987, DisplayName = "Up_PastAllIn_ClampsToTheOddAllIn")]
    public void Step_MovesToTheNextMultipleOfTheSmallBlind(int amount, int direction, int expected)
    {
        RaiseSizing.Step(amount, direction, 50m, 200m, 987m).Should().Be(expected);
    }

    [TestMethod]
    [DataRow("250", 250, null, DisplayName = "Multiple")]
    [DataRow(" 987 ", 987, null, DisplayName = "OddAllIn")]
    [DataRow("201", null, "Raise in steps of 50", DisplayName = "OffTheStep")]
    [DataRow("150", null, "The minimum raise is to 200", DisplayName = "BelowMin")]
    [DataRow("1000", null, "All-in is 987", DisplayName = "AboveAllIn")]
    [DataRow("abc", null, "Enter an amount", DisplayName = "NotANumber")]
    [DataRow("", null, "Enter an amount", DisplayName = "Empty")]
    public void Validate_ReadsTheAmountBox(string text, int? expectedAmount, string? expectedError)
    {
        // Act
        var input = RaiseSizing.Validate(text, 50m, 200m, 987m);

        // Assert
        input.Amount.Should().Be(expectedAmount);
        input.Error.Should().Be(expectedError);
        input.IsValid.Should().Be(expectedError is null);
    }

    [TestMethod]
    public void Validate_ReadsACommaAsTheDecimalPoint()
    {
        RaiseSizing.Validate("1,25", 0.25m, 0.75m, 50m).Amount.Should().Be(1.25m);
        RaiseSizing.Validate("1,3", 0.25m, 0.75m, 50m).Error.Should().Be("Raise in steps of 0.25");
    }

    [TestMethod]
    [DataRow("333", 350, DisplayName = "RoundsToTheNearestStep")]
    [DataRow("320", 300, DisplayName = "RoundsDown")]
    [DataRow("99999", 987, DisplayName = "ClampsToAllIn")]
    [DataRow("-5", 200, DisplayName = "ClampsToMin")]
    [DataRow("abc", 450, DisplayName = "NotANumber_KeepsTheLastLegalAmount")]
    public void Correct_AlwaysGivesALegalAmount(string text, int expected)
    {
        // Act
        var corrected = RaiseSizing.Correct(text, 450m, 50m, 200m, 987m);

        // Assert
        corrected.Should().Be(expected);
        RaiseSizing.Validate(corrected.ToString(System.Globalization.CultureInfo.InvariantCulture), 50m, 200m, 987m).IsValid.Should().BeTrue();
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
