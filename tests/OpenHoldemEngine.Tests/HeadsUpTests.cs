using FluentAssertions;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.Tests;

/// <summary>
/// Heads-up play, which nothing else in this suite reaches: <see cref="HoldemEngineTests"/> and
/// <see cref="MultiHandSessionTests"/> both seat six players, and the PHH corpus is six-handed
/// throughout. Two seats do not merely shorten the ring, they change the positions — the button
/// posts the small blind, acts first pre-flop and last on every street after it — so the seat on the
/// clock is asserted street by street here. The short-stack tests at the bottom are the ones that
/// matter most: heads-up is where a stack below one big blind is normal rather than exotic.
/// </summary>
[TestClass]
public sealed class HeadsUpTests
{
    private const decimal SmallBlind = 50M;
    private const decimal BigBlind = SmallBlind * 2;
    private const decimal DeepStack = 10_000M;

    /// <summary>The button, which heads-up is also the small blind.</summary>
    private const byte ButtonSeatNo = 1;

    private const byte BigBlindSeatNo = 2;

    /// <summary>
    /// One fixed deal for every hand of every test. Seat 1 ends up with the better hand on this
    /// board (ace high against queen high), which is what the showdown tests rely on.
    /// </summary>
    private static readonly Card[] _buttonHole = CardsFrom("ac", "kc");
    private static readonly Card[] _bigBlindHole = CardsFrom("7d", "2h");
    private static readonly Card[] _board = CardsFrom("4s", "9h", "qd", "3c", "8s");

    [TestMethod]
    public async Task HeadsUp_StartHand_ButtonPostsTheSmallBlindAndActsFirst()
    {
        using var session = await HeadsUpSession.CreateAsync();

        var actualGameData = await session.StartHandAsync();

        actualGameData.ShouldAsJToken().BeEquivalentToAsJToken(new GameData(
            GameId: HeadsUpSession.GameId,
            Options: HeadsUpSession.Options,
            Version: 2,
            HandData: new HandData(
                No: 1,
                Seats:
                [
                    new(ButtonSeatNo, "Button", DeepStack, DeepStack - SmallBlind)
                    {
                        BetSize = SmallBlind,
                        Hole = _buttonHole,
                        LastAction = new PlayerActionData(Street.PreFlop, HandActionType.SmallBlind, SmallBlind, 1) { SeatNo = ButtonSeatNo, Amount = SmallBlind }
                    },
                    new(BigBlindSeatNo, "BigBlind", DeepStack, DeepStack - BigBlind)
                    {
                        BetSize = BigBlind,
                        Hole = _bigBlindHole,
                        LastAction = new PlayerActionData(Street.PreFlop, HandActionType.BigBlind, BigBlind, 2) { SeatNo = BigBlindSeatNo, Amount = BigBlind }
                    },
                ],
                DealerSeatNo: ButtonSeatNo,
                SmallBlindSeatNo: ButtonSeatNo,
                BigBlindSeatNo: BigBlindSeatNo,
                NextPlayerSeatNo: ButtonSeatNo,
                Street: Street.PreFlop,
                StreetPot: SmallBlind + BigBlind,
                Pot: 0,
                CommunityCards: [],
                Evaluations:
                [
                    new(PlayerActionType.Fold),
                    new(PlayerActionType.Call, SmallBlind, SmallBlind),

                    // The big blind's post counts as a bet of 2·sb raised by sb, so the smallest
                    // legal re-raise brings the street total to 3·sb.
                    new(PlayerActionType.Raise, SmallBlind * 3, DeepStack)
                ])
            {
                SmallBlind = SmallBlind,
                BigBlind = BigBlind,
                Actions =
                [
                    new PlayerActionData(Street.PreFlop, HandActionType.SmallBlind, SmallBlind, 1) { SeatNo = ButtonSeatNo, Amount = SmallBlind },
                    new PlayerActionData(Street.PreFlop, HandActionType.BigBlind, BigBlind, 2) { SeatNo = BigBlindSeatNo, Amount = BigBlind }
                ]
            }));
    }

    [TestMethod]
    public async Task HeadsUp_BetSize_IsTheStreetWagerAfterABlindAndARaise()
    {
        using var session = await HeadsUpSession.CreateAsync();

        var handData = (await session.StartHandAsync()).HandData!;
        BetSizeOf(handData, ButtonSeatNo).Should().Be(SmallBlind, "the small blind is a wager on the street");
        BetSizeOf(handData, BigBlindSeatNo).Should().Be(BigBlind);

        handData = (await session.ActAsync(PlayerActionType.Raise, BigBlind * 3)).HandData!;
        BetSizeOf(handData, ButtonSeatNo).Should().Be(BigBlind * 3, "a raise is cumulative for the street, not the increment");
        BetSizeOf(handData, BigBlindSeatNo).Should().Be(BigBlind);
    }

    [TestMethod]
    public async Task HeadsUp_BetSize_IsNullForEverySeatOnANewStreet()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();
        await session.ActAsync(PlayerActionType.Call);

        var handData = (await session.ActAsync(PlayerActionType.Check)).HandData!;
        handData.Street.Should().Be(Street.Flop);
        handData.Seats.Should().OnlyContain(seat => seat.BetSize == null, "nobody has wagered on the flop yet");

        handData = (await session.ActAsync(PlayerActionType.Raise, BigBlind)).HandData!;
        BetSizeOf(handData, BigBlindSeatNo).Should().Be(BigBlind);
        BetSizeOf(handData, ButtonSeatNo).Should().BeNull("the button has not acted on the flop");
    }

    [TestMethod]
    public async Task HeadsUp_HandResultDescription_NamesTheHandAtShowdown()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();
        await session.ActAsync(PlayerActionType.Call);
        var gameData = await session.ActAsync(PlayerActionType.Check);
        for (var check = 0; check < 6; check++)
        {
            gameData = await session.ActAsync(PlayerActionType.Check);
        }

        // Neither hand pairs the board: ace high against seven high.
        gameData.HandData!.Seats.Select(seat => seat.HandResult!.RankType).Should().OnlyContain(rank => rank == HandRankType.HighCard);
        gameData.HandData.Seats.Select(seat => seat.HandResult!.Description).Should().OnlyContain(description => description == "High card");
    }

    [TestMethod]
    public async Task HeadsUp_HandResultDescription_IsNullWhenTheHandNeverReachedShowdown()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();

        var handData = (await session.ActAsync(PlayerActionType.Fold)).HandData!;

        handData.Seats.Should().OnlyContain(seat => seat.HandResult != null && seat.HandResult.Description == null,
            "the folder never showed a hand and the winner collected without one");
    }

    [TestMethod]
    public async Task HeadsUp_BigBlindGetsTheOptionAfterTheButtonCalls()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();

        var handData = (await session.ActAsync(PlayerActionType.Call)).HandData!;

        handData.Street.Should().Be(Street.PreFlop, "the big blind's option keeps the street open");
        handData.NextPlayerSeatNo.Should().Be(BigBlindSeatNo);
        handData.CommunityCards.Should().BeEmpty();
        handData.StreetPot.Should().Be(BigBlind * 2);
        handData.Evaluations.Select(evaluation => evaluation.PlayerActionType).Should()
            .Equal([PlayerActionType.Fold, PlayerActionType.Check, PlayerActionType.Raise],
                "the big blind has nothing left to call, but may still raise its own option");
    }

    [TestMethod]
    public async Task HeadsUp_BigBlindRaisesTheOption_ButtonIsAskedAgain()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();
        await session.ActAsync(PlayerActionType.Call);

        var handData = (await session.ActAsync(PlayerActionType.Raise, SmallBlind * 3)).HandData!;

        handData.Street.Should().Be(Street.PreFlop);
        handData.NextPlayerSeatNo.Should().Be(ButtonSeatNo, "a raise puts the button back on the clock");
        handData.GetEvalMinValue(PlayerActionType.Call).Should().Be(SmallBlind,
            "the button has 2·sb in and owes the difference up to 3·sb");
    }

    [TestMethod]
    public async Task HeadsUp_BigBlindActsFirstOnEveryStreetAfterThePreFlop()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();
        await session.ActAsync(PlayerActionType.Call);

        // Closing the pre-flop is the big blind's check, which is the last time it acts second.
        var handData = (await session.ActAsync(PlayerActionType.Check)).HandData!;

        foreach (var (street, boardSize) in new[] { (Street.Flop, 3), (Street.Turn, 4), (Street.River, 5) })
        {
            handData.Street.Should().Be(street);
            handData.CommunityCards.Should().Equal(_board.Take(boardSize), $"the board on the {street}");
            handData.NextPlayerSeatNo.Should().Be(BigBlindSeatNo, $"the big blind acts first on the {street}");

            handData = (await session.ActAsync(PlayerActionType.Check)).HandData!;
            handData.NextPlayerSeatNo.Should().Be(ButtonSeatNo, $"the button acts last on the {street}");

            handData = (await session.ActAsync(PlayerActionType.Check)).HandData!;
        }

        handData.Seats.Should().OnlyContain(seat => seat.HandResult != null, "the river check ends the hand");
    }

    [TestMethod]
    public async Task HeadsUp_ButtonFoldsPreFlop_BigBlindCollectsTheSmallBlind()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();

        var handData = (await session.ActAsync(PlayerActionType.Fold)).HandData!;

        handData.CommunityCards.Should().BeEmpty("a hand decided pre-flop deals no board");
        NetOf(handData, ButtonSeatNo).Should().Be(-SmallBlind, "the button folded its small blind away");
        NetOf(handData, BigBlindSeatNo).Should().Be(SmallBlind, "the big blind collects the small blind");
    }

    [TestMethod]
    public async Task HeadsUp_ShowdownAwardsThePotAndConservesChips()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();
        await session.ActAsync(PlayerActionType.Call);

        var gameData = await session.ActAsync(PlayerActionType.Check);
        for (var check = 0; check < 6; check++)
        {
            gameData = await session.ActAsync(PlayerActionType.Check);
        }

        var handData = gameData.HandData!;
        handData.CommunityCards.Should().Equal(_board);
        NetOf(handData, ButtonSeatNo).Should().Be(BigBlind, "ace high takes the whole 2·bb pot");
        NetOf(handData, BigBlindSeatNo).Should().Be(-BigBlind);
        handData.Seats.Sum(seat => seat.HandResult!.Net).Should().Be(0, "a hand neither creates nor destroys chips");
    }

    /// <summary>
    /// <see cref="HandData.StreetPot"/> is the street on the clock and <see cref="HandData.Pot"/> the
    /// streets before it, so the two never overlap. A new street opens with nothing on it, and a
    /// finished hand has everything in <see cref="HandData.Pot"/> — at both points the street that just
    /// closed used to be counted in each of them, doubling the pot a pot-sized raise is built from.
    /// </summary>
    [TestMethod]
    public async Task HeadsUp_StreetPotIsOnlyTheStreetOnTheClock()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();
        await session.ActAsync(PlayerActionType.Call);

        var handData = (await session.ActAsync(PlayerActionType.Check)).HandData!;

        handData.Street.Should().Be(Street.Flop);
        handData.StreetPot.Should().Be(0, "nobody has put anything in on the flop yet");
        handData.Pot.Should().Be(BigBlind * 2, "the pre-flop is a prior street now");

        handData = (await session.ActAsync(PlayerActionType.Raise, BigBlind * 2)).HandData!;
        handData.StreetPot.Should().Be(BigBlind * 2, "the big blind's flop bet");
        handData.Pot.Should().Be(BigBlind * 2);

        handData = (await session.ActAsync(PlayerActionType.Call)).HandData!;
        handData.Street.Should().Be(Street.Turn);
        handData.StreetPot.Should().Be(0, "nobody has put anything in on the turn yet");
        handData.Pot.Should().Be(BigBlind * 6);

        for (var check = 0; check < 4; check++)
        {
            handData = (await session.ActAsync(PlayerActionType.Check)).HandData!;
        }

        handData.Street.Should().Be(Street.Finished);
        handData.StreetPot.Should().Be(0, "a finished hand has no street on the clock");
        handData.Pot.Should().Be(BigBlind * 6, "everything wagered, counted once");
    }

    [TestMethod]
    public async Task HeadsUp_ButtonAlternatesEveryHand()
    {
        using var session = await HeadsUpSession.CreateAsync();

        for (byte expectedButtonSeatNo = 1; expectedButtonSeatNo <= 2; expectedButtonSeatNo++)
        {
            var handData = (await session.StartHandAsync()).HandData!;

            handData.DealerSeatNo.Should().Be(expectedButtonSeatNo, "the button alternates heads-up");
            handData.SmallBlindSeatNo.Should().Be(expectedButtonSeatNo, "the button posts the small blind");
            handData.BigBlindSeatNo.Should().Be(OtherSeatNo(expectedButtonSeatNo));
            handData.NextPlayerSeatNo.Should().Be(expectedButtonSeatNo, "the button acts first pre-flop");

            await session.ActAsync(PlayerActionType.Fold);
        }
    }

    /// <summary>
    /// The big blind cannot cover its own blind. It posts what it has and is all in; the button owes
    /// only that much to call, and has to be asked for it rather than having the board run out from
    /// under it.
    /// </summary>
    [TestMethod]
    public async Task HeadsUp_BigBlindShortOfTheBigBlind_PostsAllInAndTheButtonIsAsked()
    {
        const decimal shortStack = 60M;
        using var session = await HeadsUpSession.CreateAsync(bigBlindStack: shortStack);

        var handData = (await session.StartHandAsync()).HandData!;

        handData.SmallBlindSeatNo.Should().Be(ButtonSeatNo);
        handData.BigBlindSeatNo.Should().Be(BigBlindSeatNo);
        handData.StreetPot.Should().Be(SmallBlind + shortStack, "the big blind posted only what it had");
        SeatOf(handData, BigBlindSeatNo).Stack.Should().Be(0, "posting the blind put it all in");
        SeatOf(handData, BigBlindSeatNo).IsAllIn.Should().BeTrue();
        handData.NextPlayerSeatNo.Should().Be(ButtonSeatNo, "the button still owes the difference");
        handData.GetEvalMinValue(PlayerActionType.Call).Should().Be(shortStack - SmallBlind);

        handData = (await session.ActAsync(PlayerActionType.Call)).HandData!;

        handData.CommunityCards.Should().Equal(_board, "with the calling seat all in the board runs out in full");
        NetOf(handData, ButtonSeatNo).Should().Be(shortStack, "ace high wins the short stack's whole commitment");
        NetOf(handData, BigBlindSeatNo).Should().Be(-shortStack);
        handData.Seats.Should().OnlyContain(seat => seat.Stack >= 0, "no seat can wager more than it has");
    }

    /// <summary>
    /// The button cannot cover the big blind. It posts the small blind in full, calls all in for the
    /// rest, and the part of the big blind that was never matched comes back to the big blind — the
    /// hand is playable, not a failure to deal.
    /// </summary>
    [TestMethod]
    public async Task HeadsUp_ButtonShortOfTheBigBlind_PlaysAndReturnsTheUncalledExcess()
    {
        const decimal shortStack = 60M;
        using var session = await HeadsUpSession.CreateAsync(buttonStack: shortStack);

        var handData = (await session.StartHandAsync()).HandData!;

        handData.StreetPot.Should().Be(SmallBlind + BigBlind, "the button could still cover the small blind");
        SeatOf(handData, ButtonSeatNo).Stack.Should().Be(shortStack - SmallBlind);
        handData.NextPlayerSeatNo.Should().Be(ButtonSeatNo);
        handData.GetEvalMaxValue(PlayerActionType.Raise).Should().Be(shortStack,
            "a raise bound is a street total, and the button's whole stack only reaches the short of a full blind");

        handData = (await session.ActAsync(PlayerActionType.Call)).HandData!;

        handData.CommunityCards.Should().Equal(_board);
        NetOf(handData, ButtonSeatNo).Should().Be(shortStack, "the button doubles the stack it was all in for");
        NetOf(handData, BigBlindSeatNo).Should().Be(-shortStack,
            "only what the button could match was ever at risk — the rest of the big blind comes back");
        handData.Seats.Should().OnlyContain(seat => seat.Stack >= 0);
    }

    /// <summary>
    /// A fold row carries <c>Amount = 0</c>, so a stack built from the last row of each street would
    /// forget what the folder had already put in on the street it folded on.
    /// </summary>
    [TestMethod]
    public async Task HeadsUp_Stack_KeepsTheBlindOfASeatThatFoldsPreFlopToARaise()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();
        await session.ActAsync(PlayerActionType.Raise, BigBlind * 3);

        var handData = (await session.ActAsync(PlayerActionType.Fold)).HandData!;

        SeatOf(handData, BigBlindSeatNo).Stack.Should().Be(DeepStack - BigBlind, "the big blind is gone for good once it folds");
        SeatOf(handData, ButtonSeatNo).Stack.Should().Be(DeepStack - BigBlind * 3);
    }

    [TestMethod]
    public async Task HeadsUp_Stack_KeepsEveryStreetWagerOfASeatThatFoldsOnTheTurn()
    {
        using var session = await HeadsUpSession.CreateAsync();
        await session.StartHandAsync();
        await session.ActAsync(PlayerActionType.Call);
        await session.ActAsync(PlayerActionType.Check);

        // Flop: the big blind acts first, bets, and is called.
        await session.ActAsync(PlayerActionType.Raise, BigBlind);
        await session.ActAsync(PlayerActionType.Call);

        // Turn: the big blind bets again, is raised, and folds.
        await session.ActAsync(PlayerActionType.Raise, BigBlind);
        await session.ActAsync(PlayerActionType.Raise, BigBlind * 3);
        var handData = (await session.ActAsync(PlayerActionType.Fold)).HandData!;

        SeatOf(handData, BigBlindSeatNo).Stack.Should().Be(DeepStack - BigBlind * 3, "pre-flop, flop and turn wagers of 100 each");
        SeatOf(handData, BigBlindSeatNo).IsAllIn.Should().BeFalse();
        SeatOf(handData, ButtonSeatNo).Stack.Should().Be(DeepStack - BigBlind * 5);
    }

    /// <summary>
    /// When the big blind posts less than a full blind, <see cref="HandData.BigBlind"/> is still the
    /// nominal <c>2 × InitialSmallBlind</c> while the seat's <see cref="SeatData.LastAction"/> carries
    /// the nominal level too — not what was actually posted. The stack reaches 0 because the seat
    /// posted everything it had.
    /// </summary>
    [TestMethod]
    public async Task HeadsUp_BigBlind_IsNominalEvenWhenTheShortBigBlindPostsLess()
    {
        const decimal shortStack = 60M;
        using var session = await HeadsUpSession.CreateAsync(bigBlindStack: shortStack);

        var handData = (await session.StartHandAsync()).HandData!;

        handData.BigBlind.Should().Be(BigBlind, "the big blind nominal is always 2 × InitialSmallBlind");
        SeatOf(handData, BigBlindSeatNo).LastAction!.Size.Should().Be(BigBlind, "the posted amount size is the nominal blind");
        SeatOf(handData, BigBlindSeatNo).LastAction!.Amount.Should().Be(shortStack, "the amount posted is what the seat had");
        SeatOf(handData, BigBlindSeatNo).Stack.Should().Be(0M, "the short stack was exhausted posting what it could");
    }

    private static SeatData SeatOf(HandData handData, byte seatNo)
        => handData.Seats.Single(seat => seat.SeatNo == seatNo);

    private static decimal? BetSizeOf(HandData handData, byte seatNo)
        => SeatOf(handData, seatNo).BetSize;

    private static decimal NetOf(HandData handData, byte seatNo)
        => SeatOf(handData, seatNo).HandResult!.Net;

    private static byte OtherSeatNo(byte seatNo) => (byte)(seatNo == ButtonSeatNo ? BigBlindSeatNo : ButtonSeatNo);

    private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];

    /// <summary>
    /// A two-seat game over the shared test host, dealing the same scripted cards every hand.
    /// Every test here needs the same three calls to get going, so they live in one place.
    /// </summary>
    private sealed class HeadsUpSession : IDisposable
    {
        internal static readonly Guid GameId = Guid.Parse("{33333333-3333-3333-3333-333333333333}");

        internal static readonly GameOptionData Options = new() { MaxPlayersCount = 2, InitialSmallBlind = SmallBlind, InitialDealerSeatNo = ButtonSeatNo };

        private readonly TestEngineHost.DisposableTestData _host;
        private readonly HoldemEngine _engine;
        private readonly PlayerJoinData[] _joinSeats;
        private bool _seated;

        private HeadsUpSession(decimal buttonStack, decimal bigBlindStack)
        {
            _joinSeats =
            [
                new("Button", ButtonSeatNo, buttonStack),
                new("BigBlind", BigBlindSeatNo, bigBlindStack)
            ];
            _host = TestEngineHost.Setup(out _engine, out _, shuffler: new TestCardShuffler(
                getHoleFunc: (_, seatNo) => seatNo == ButtonSeatNo ? _buttonHole : _bigBlindHole,
                getDeckFunc: _ => _board));
        }

        internal static async Task<HeadsUpSession> CreateAsync(
            decimal buttonStack = DeepStack, decimal bigBlindStack = DeepStack)
        {
            var session = new HeadsUpSession(buttonStack, bigBlindStack);
            await session.ApplyAsync(new HoldemActionCreateGame(GameId, Options));
            return session;
        }

        internal async Task<GameData> StartHandAsync()
        {
            HoldemAction action = _seated
                ? new HoldemActionStartHand(GameId)
                : new HoldemActionStartGame(GameId, _joinSeats);
            _seated = true;
            return await ApplyAsync(action);
        }

        internal Task<GameData> ActAsync(PlayerActionType actionType, decimal size = 0)
            => ApplyAsync(new HoldemActionPlay(GameId, actionType, size));

        private async Task<GameData> ApplyAsync(HoldemAction holdemAction)
        {
            var result = await _engine.ApplyHoldemActionAsync(holdemAction);

            result.IsSuccess.Should().BeTrue(
                $"{holdemAction.GetType().Name} should be accepted: " +
                string.Join("; ", result.Errors.Select(error => error.Message)));

            return result.Value;
        }

        public void Dispose() => _host.Dispose();
    }
}
