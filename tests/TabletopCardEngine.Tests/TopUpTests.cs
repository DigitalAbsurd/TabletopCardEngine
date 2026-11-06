using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Diagnostics;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Adding chips to an occupied seat between hands with <see cref="HoldemActionTopUp"/>: a top-up while the seat
/// has chips, a rebuy once it has none. The chips are written for the next hand and dealt by <c>StartHand</c>.
/// </summary>
[TestClass]
public sealed class TopUpTests
{
    private const decimal SmallBlind = 5M;
    private const decimal Stack1000 = 1000M;

    private static readonly PlayerJoinData[] _threeSeats =
    [
        new("Player 1", 1, Stack1000),
        new("Player 2", 2, Stack1000),
        new("Player 3", 3, Stack1000),
    ];

    private static readonly PlayerJoinData[] _headsUpSeats =
    [
        new("Player 1", 1, 100M),
        new("Player 2", 2, Stack1000),
    ];

    private static readonly GameOptionData _sixMaxTopUp = new()
    {
        MaxPlayersCount = 6,
        InitialSmallBlind = SmallBlind,
        InitialDealerSeatNo = 1,
        AllowTopUp = true,
    };

    private static readonly GameOptionData _headsUpRebuy = new()
    {
        TableFormat = HoldemTableFormat.HeadsUp,
        MaxPlayersCount = 2,
        InitialSmallBlind = SmallBlind,
        InitialDealerSeatNo = 1,
        AllowRebuy = true,
    };

    [TestMethod]
    public async Task TopUp_BetweenHands_NextHandStartsWithFinishStackPlusAmount()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        var hand1 = await session.PlayHandToEndAsync();
        var finishStack = FinishStack(hand1, 2);
        finishStack.Should().Be(995M, "seat 2 posted the small blind and folded");

        var afterTopUp = await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 100M));

        afterTopUp.HandData!.No.Should().Be(1);
        FinishStack(afterTopUp.HandData, 2).Should().Be(finishStack, "the chips are not added to the hand that ended");
        (await session.LoadBuyInsAsync()).Should().ContainSingle(buyIn => buyIn.Kind == BuyInKind.TopUp)
            .Which.Should().Be(((byte)2, 2, 100M, BuyInKind.TopUp));

        var hand2 = await session.StartHandAsync();

        hand2.No.Should().Be(2);
        hand2.Seats.Single(seat => seat.SeatNo == 2).StartHandStack.Should().Be(finishStack + 100M);
        hand2.Seats.Where(seat => seat.SeatNo != 2).Select(seat => seat.StartHandStack)
            .Should().Equal(FinishStack(hand1, 1), FinishStack(hand1, 3));
    }

    [TestMethod]
    public async Task TopUp_TwiceBetweenTheSameHands_AddsUp()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        var hand1 = await session.PlayHandToEndAsync();

        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 100M));
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 50.5M));

        (await session.LoadBuyInsAsync()).Where(buyIn => buyIn.HandNo == 2)
            .Should().Equal(((byte)2, 2, 150.5M, BuyInKind.TopUp));

        var hand2 = await session.StartHandAsync();

        hand2.Seats.Single(seat => seat.SeatNo == 2).StartHandStack.Should().Be(FinishStack(hand1, 2) + 150.5M);
    }

    [TestMethod]
    public async Task TopUp_DuringAHand_FailsWithPreviousHandNotEndedAndSavesNothing()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        var before = await session.GetGameAsync();
        var rowsBefore = await session.CountRowsAsync();

        var result = await session.ApplyRawAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 100M));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.PreviousHandNotEnded);
        (await session.CountRowsAsync()).Should().Equal(rowsBefore);
        (await session.GetGameAsync()).Should().Be(before);
    }

    [TestMethod]
    public async Task TopUp_BeforeStartGame_FailsWithGameNotStarted()
    {
        using var session = await TopUpSession.CreateUnstartedAsync(_sixMaxTopUp);

        var result = await session.ApplyRawAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameNotStarted);
        (await session.GetGameAsync()).Version.Should().Be(1, "nothing was saved");
    }

    [TestMethod]
    [DataRow((byte)5, false, DisplayName = "Empty seat")]
    [DataRow((byte)0, false, DisplayName = "Seat 0")]
    [DataRow((byte)7, false, DisplayName = "Seat above MaxPlayersCount")]
    [DataRow((byte)2, true, DisplayName = "Seat whose player left")]
    public async Task TopUp_SeatWithNobodyInIt_FailsWithInvalidSeat(byte seatNo, bool leaveFirst)
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        await session.PlayHandToEndAsync();
        if (leaveFirst)
        {
            await session.ApplyOkAsync(HoldemAction.LeaveSeat(TopUpSession.GameId, seatNo));
        }

        var before = await session.GetGameAsync();
        var rowsBefore = await session.CountRowsAsync();

        var result = await session.ApplyRawAsync(HoldemAction.TopUp(TopUpSession.GameId, seatNo, 100M));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidSeat);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await session.CountRowsAsync()).Should().Equal(rowsBefore);
        (await session.GetGameAsync()).Should().Be(before);
    }

    [TestMethod]
    [DataRow("0", DisplayName = "Zero")]
    [DataRow("-10", DisplayName = "Negative")]
    [DataRow("10.001", DisplayName = "Three decimal places")]
    public async Task TopUp_AmountNotAPositiveCentAmount_FailsWithInvalidBuyIn(string amount)
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        await session.PlayHandToEndAsync();
        var rowsBefore = await session.CountRowsAsync();

        var result = await session.ApplyRawAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidBuyIn);
        (await session.CountRowsAsync()).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task TopUp_NotAllowed_FailsWithTopUpNotAllowed()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp with { AllowTopUp = false, AllowRebuy = true });
        await session.PlayHandToEndAsync();
        var rowsBefore = await session.CountRowsAsync();

        var result = await session.ApplyRawAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 100M));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.TopUpNotAllowed, "a seat with chips is topping up, not rebuying");
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await session.CountRowsAsync()).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task TopUp_AboveMaxBuyIn_FailsWithInvalidBuyIn_AndExactlyToMaxBuyInIsAccepted()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp with { MaxBuyIn = 1100M });
        var hand1 = await session.PlayHandToEndAsync();
        FinishStack(hand1, 2).Should().Be(995M);

        var above = await session.ApplyRawAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 105.01M));
        above.IsFailed.Should().BeTrue();
        above.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidBuyIn);

        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 105M));

        var pastPending = await session.ApplyRawAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 0.01M));
        pastPending.IsFailed.Should().BeTrue();
        pastPending.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidBuyIn, "the pending top-up already takes the seat to MaxBuyIn");

        var hand2 = await session.StartHandAsync();
        hand2.Seats.Single(seat => seat.SeatNo == 2).StartHandStack.Should().Be(1100M);
    }

    [TestMethod]
    public async Task TopUp_BustedHeadsUpSeat_RebuysAndTheNextHandIsDealtToBoth()
    {
        using var session = await TopUpSession.CreateAsync(_headsUpSeats, _headsUpRebuy);
        await session.BustSeat1Async();
        var rowsBefore = await session.CountRowsAsync();

        var notEnough = await session.ApplyRawAsync(HoldemAction.StartHand(TopUpSession.GameId));
        notEnough.IsFailed.Should().BeTrue();
        notEnough.Errors.Single().Code.Should().Be(HoldemErrorCode.NotEnoughPlayers);
        (await session.CountRowsAsync()).Should().Equal(rowsBefore);

        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M));
        (await session.LoadBuyInsAsync()).Where(buyIn => buyIn.HandNo == 2)
            .Should().Equal(((byte)1, 2, 100M, BuyInKind.Rebuy));

        var hand2 = await session.StartHandAsync();

        hand2.No.Should().Be(2);
        hand2.Seats.Select(seat => (seat.SeatNo, seat.StartHandStack)).Should().Equal(((byte)1, 100M), ((byte)2, 1100M));
        hand2.Seats.Should().OnlyContain(seat => seat.Hole!.Count == 2);
    }

    [TestMethod]
    public async Task TopUp_BustedSeatWithRebuyNotAllowed_FailsWithRebuyNotAllowed()
    {
        using var session = await TopUpSession.CreateAsync(_headsUpSeats, _headsUpRebuy with { AllowRebuy = false, AllowTopUp = true });
        await session.BustSeat1Async();
        var rowsBefore = await session.CountRowsAsync();

        var result = await session.ApplyRawAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.RebuyNotAllowed, "a busted seat is rebuying, not topping up");
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await session.CountRowsAsync()).Should().Equal(rowsBefore);
    }

    [TestMethod]
    public async Task TopUp_MaxRebuysReached_FailsWithRebuyNotAllowed_UntilANewPlayerTakesTheSeat()
    {
        using var session = await TopUpSession.CreateAsync(_headsUpSeats, _headsUpRebuy with { MaxRebuys = 1 });
        await session.BustSeat1Async();
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M));
        await session.StartHandAsync();

        // Hand 2: the button moved to seat 2, which acts first pre-flop and shoves its aces.
        await session.ApplyOkAsync(HoldemAction.Play(TopUpSession.GameId, PlayerActionType.AllIn));
        var bust = await session.ApplyOkAsync(HoldemAction.Play(TopUpSession.GameId, PlayerActionType.Call));
        FinishStack(bust.HandData!, 1).Should().Be(0M, "seat 1 is busted again");

        var second = await session.ApplyRawAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M));
        second.IsFailed.Should().BeTrue();
        second.Errors.Single().Code.Should().Be(HoldemErrorCode.RebuyNotAllowed);

        await session.ApplyOkAsync(HoldemAction.LeaveSeat(TopUpSession.GameId, 1));
        await session.StartHandAsync([new("Player 9", 1, 100M)]);
        await session.BustSeat1Async();

        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M));
        (await session.LoadBuyInsAsync()).Where(buyIn => buyIn.SeatNo == 1).Select(buyIn => (buyIn.HandNo, buyIn.Kind))
            .Should().Equal((1, BuyInKind.Join), (2, BuyInKind.Rebuy), (3, BuyInKind.Join), (4, BuyInKind.Rebuy));
    }

    [TestMethod]
    [DataRow("49.99", false, DisplayName = "Below MinBuyIn")]
    [DataRow("1000.01", false, DisplayName = "Above MaxBuyIn")]
    [DataRow("50", true, DisplayName = "At MinBuyIn")]
    [DataRow("1000", true, DisplayName = "At MaxBuyIn")]
    public async Task TopUp_RebuyOutsideTheBuyInWindow_FailsWithInvalidBuyIn(string amount, bool accepted)
    {
        using var session = await TopUpSession.CreateAsync(_headsUpSeats, _headsUpRebuy with { MinBuyIn = 50M, MaxBuyIn = Stack1000 });
        await session.BustSeat1Async();

        var result = await session.ApplyRawAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

        if (accepted)
        {
            result.IsSuccess.Should().BeTrue();
        }
        else
        {
            result.IsFailed.Should().BeTrue();
            result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidBuyIn);
        }
    }

    [TestMethod]
    public async Task TopUp_ThenLeaveSeat_CancelsThePendingChips()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        await session.PlayHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 100M));

        await session.ApplyOkAsync(HoldemAction.LeaveSeat(TopUpSession.GameId, 2));

        (await session.LoadBuyInsAsync()).Should().OnlyContain(buyIn => buyIn.Kind == BuyInKind.Join && buyIn.HandNo == 1);

        var hand2 = await session.StartHandAsync();
        hand2.Seats.Select(seat => seat.SeatNo).Should().Equal(1, 3);
    }

    [TestMethod]
    public async Task TopUp_ThenFinishGame_CancelsThePendingChips()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        await session.PlayHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 100M));

        await session.ApplyOkAsync(HoldemAction.FinishGame(TopUpSession.GameId));

        (await session.LoadBuyInsAsync()).Should().OnlyContain(buyIn => buyIn.Kind == BuyInKind.Join && buyIn.HandNo == 1);
    }

    [TestMethod]
    public async Task TopUp_AcrossHands_ConservesChips()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        await session.PlayHandToEndAsync();

        var accepted = 0M;
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 200M));
        accepted += 200M;
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 5M));
        accepted += 5M;

        var afterLeave = (await session.ApplyOkAsync(HoldemAction.LeaveSeat(TopUpSession.GameId, 3))).HandData!;
        var leaverStack = FinishStack(afterLeave, 3);

        await session.StartHandAsync([new("Player 9", 5, 700M)]);
        await session.PlayHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 5, 300M));
        accepted += 300M;
        await session.StartHandAsync();
        var finished = await session.PlayHandToEndAsync();

        const decimal joins = (3 * Stack1000) + 700M;
        var seatedStacks = finished.Seats.Sum(seat => seat.StartHandStack + seat.HandResult!.Net);

        (joins + accepted).Should().Be(seatedStacks + leaverStack);
        (await session.LoadBuyInsAsync()).Sum(buyIn => buyIn.Amount).Should().Be(joins + accepted);
    }

    [TestMethod]
    public async Task GameData_AfterTopUp_ShowsPendingBuyInAndZeroRebuys()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        await session.PlayHandToEndAsync();

        var afterTopUp = await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 100M));

        afterTopUp.HandData!.Seats.Single(seat => seat.SeatNo == 2).Should().Match<SeatData>(seat =>
            seat.PendingBuyIn == 100M && seat.Rebuys == 0);
    }

    [TestMethod]
    public async Task GameData_AfterTwoTopUps_ShowsCombinedPendingBuyIn()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        await session.PlayHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 100M));

        var afterSecond = await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 50.5M));

        afterSecond.HandData!.Seats.Single(seat => seat.SeatNo == 2).Should().Match<SeatData>(seat =>
            seat.PendingBuyIn == 150.5M && seat.Rebuys == 0);
    }

    [TestMethod]
    public async Task GameData_NextHandAfterTopUp_PendingBuyInGoesToZero()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        var hand1 = await session.PlayHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 100M));

        var hand2 = await session.StartHandAsync();

        hand2.Seats.Single(seat => seat.SeatNo == 2).Should().Match<SeatData>(seat =>
            seat.PendingBuyIn == 0 && seat.Rebuys == 0);
    }

    [TestMethod]
    public async Task GameData_AfterTopUpThenLeaveSeat_PendingBuyInGoesToZero()
    {
        using var session = await TopUpSession.CreateAsync(_threeSeats, _sixMaxTopUp);
        await session.PlayHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 2, 100M));

        var afterLeave = await session.ApplyOkAsync(HoldemAction.LeaveSeat(TopUpSession.GameId, 2));

        // The seat still shows in the hand that ended, with HasLeft = true
        afterLeave.HandData!.Seats.Single(seat => seat.SeatNo == 2).Should().Match<SeatData>(seat =>
            seat.HasLeft && seat.PendingBuyIn == 0);
    }

    [TestMethod]
    public async Task GameData_AfterRebuy_ShowsRebuyCount()
    {
        using var session = await TopUpSession.CreateAsync(_headsUpSeats, _headsUpRebuy);
        await session.BustSeat1Async();

        var afterRebuy = await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M));

        afterRebuy.HandData!.Seats.Single(seat => seat.SeatNo == 1).Should().Match<SeatData>(seat =>
            seat.PendingBuyIn == 100M && seat.Rebuys == 1);
    }

    [TestMethod]
    public async Task GameData_RebuysCountsRebuyRowsOnly()
    {
        using var session = await TopUpSession.CreateAsync(_headsUpSeats, _headsUpRebuy with { MaxRebuys = 2 });
        await session.BustSeat1Async();
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M));
        var hand2 = await session.StartHandAsync();

        hand2.Seats.Single(seat => seat.SeatNo == 1).Rebuys.Should().Be(1);

        // Hand 2: bust and rebuy again
        await session.ApplyOkAsync(HoldemAction.Play(TopUpSession.GameId, PlayerActionType.AllIn));
        await session.ApplyOkAsync(HoldemAction.Play(TopUpSession.GameId, PlayerActionType.Call));
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M));

        var hand3 = await session.StartHandAsync();

        hand3.Seats.Single(seat => seat.SeatNo == 1).Rebuys.Should().Be(2);
    }

    [TestMethod]
    public async Task GameData_NewPlayerInSeat_RebuyCountRestarts()
    {
        using var session = await TopUpSession.CreateAsync(_headsUpSeats, _headsUpRebuy with { MaxRebuys = 2 });
        await session.BustSeat1Async();
        await session.ApplyOkAsync(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M));
        await session.StartHandAsync();
        await session.PlayHandToEndAsync();

        // Seat 1 leaves
        await session.ApplyOkAsync(HoldemAction.LeaveSeat(TopUpSession.GameId, 1));

        // New player sits at seat 1
        var hand3 = await session.StartHandAsync([new("Player 9", 1, 100M)]);

        hand3.Seats.Single(seat => seat.SeatNo == 1).Rebuys.Should().Be(0, "the new player has no rebuys yet");
    }

    [TestMethod]
    public void HoldemActionNames_TopUp_IsTopUp()
        => HoldemActionNames.Of(HoldemAction.TopUp(TopUpSession.GameId, 1, 100M)).Should().Be("TopUp");

    private static decimal FinishStack(HandData hand, byte seatNo)
    {
        var seat = hand.Seats.Single(seat => seat.SeatNo == seatNo);
        return seat.StartHandStack + seat.HandResult!.Net;
    }

    private sealed class TopUpSession : IDisposable
    {
        internal static readonly Guid GameId = Guid.Parse("{70970970-9709-7097-0970-970970970970}");

        private static readonly Card[] _board = CardsFrom("4s", "9h", "qd", "3c", "8s");

        private readonly TestEngineHost.DisposableTestData _host;
        private readonly IHoldemTable _engine;
        private readonly IDbContextFactory<HoldemDbContext> _contextFactory;

        private TopUpSession()
        {
            // Seat 1 always holds 7-2 and every other seat aces, so seat 1 loses every showdown.
            _host = TestEngineHost.Setup(out _engine, out _contextFactory, shuffler: new TestCardShuffler(
                getHoleFunc: (_, seatNo) => seatNo == 1 ? CardsFrom("7c", "2d") : CardsFrom("ac", "ad"),
                getDeckFunc: _ => _board));
        }

        internal static async Task<TopUpSession> CreateAsync(PlayerJoinData[] seats, GameOptionData options)
        {
            var session = new TopUpSession();
            await session.ApplyOkAsync(new HoldemActionCreateGame(GameId, options));
            await session.ApplyOkAsync(new HoldemActionStartGame(GameId, seats));
            return session;
        }

        internal static async Task<TopUpSession> CreateUnstartedAsync(GameOptionData options)
        {
            var session = new TopUpSession();
            await session.ApplyOkAsync(new HoldemActionCreateGame(GameId, options));
            return session;
        }

        internal async Task<HandData> StartHandAsync(PlayerJoinData[]? joinSeats = null)
            => (await ApplyOkAsync(HoldemAction.StartHand(GameId, joinSeats))).HandData!;

        /// <summary>Ends the current hand by folding every seat on the clock until one is left.</summary>
        internal async Task<HandData> PlayHandToEndAsync()
        {
            var game = await GetGameAsync();
            while (game.HandData!.Street != Street.Finished)
            {
                game = await ApplyOkAsync(HoldemAction.Play(GameId, PlayerActionType.Fold));
            }
            return game.HandData;
        }

        /// <summary>
        /// Heads-up with seat 1 on the button, which acts first pre-flop: its 7-2 shoves into aces and loses.
        /// </summary>
        internal async Task BustSeat1Async()
        {
            await ApplyOkAsync(HoldemAction.Play(GameId, PlayerActionType.AllIn));
            var bust = await ApplyOkAsync(HoldemAction.Play(GameId, PlayerActionType.Call));
            bust.HandData!.Street.Should().Be(Street.Finished);
            FinishStack(bust.HandData, 1).Should().Be(0M, "seat 1 is busted");
        }

        internal Task<GameData> GetGameAsync() => ApplyOkAsync(HoldemAction.GetGame(GameId));

        internal Task<HoldemResult<GameData>> ApplyRawAsync(HoldemAction holdemAction) => _engine.ApplyAsync(holdemAction);

        internal async Task<GameData> ApplyOkAsync(HoldemAction holdemAction)
        {
            var result = await _engine.ApplyAsync(holdemAction);

            result.IsSuccess.Should().BeTrue(
                $"{holdemAction.GetType().Name} should be accepted: " +
                string.Join("; ", result.Errors.Select(error => error.Message)));

            return result.Value;
        }

        internal async Task<List<(byte SeatNo, int HandNo, decimal Amount, BuyInKind Kind)>> LoadBuyInsAsync()
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var buyIns = await dbContext.Set<BuyIn>().AsNoTracking().ToListAsync();
            return [.. buyIns
                .OrderBy(buyIn => buyIn.HandNo).ThenBy(buyIn => buyIn.SeatNo)
                .Select(buyIn => (buyIn.SeatNo, buyIn.HandNo, buyIn.Amount, buyIn.Kind))];
        }

        internal async Task<int[]> CountRowsAsync()
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return
            [
                await dbContext.Set<Game>().CountAsync(),
                await dbContext.Set<Hand>().CountAsync(),
                await dbContext.Set<HandAction>().CountAsync(),
                await dbContext.Set<HandPlayerStartState>().CountAsync(),
                await dbContext.Set<Player>().CountAsync(),
                await dbContext.Set<BuyIn>().CountAsync(),
            ];
        }

        private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];

        public void Dispose() => _host.Dispose();
    }
}
