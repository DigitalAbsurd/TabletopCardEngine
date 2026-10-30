using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Diagnostics;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Engine;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Standing up between hands with <see cref="HoldemActionLeaveSeat"/>: the seat is dealt out of the next
/// hand, its stack leaves with the player, and a new player can take it with <c>StartHand.JoinSeats</c>.
/// </summary>
[TestClass]
public sealed class SeatLeaveTests
{
    private const decimal SmallBlind = 5M;
    private const decimal Stack1000 = 1000M;

    private static readonly PlayerJoinData[] _threeSeats =
    [
        new("Player 1", 1, Stack1000),
        new("Player 2", 2, Stack1000),
        new("Player 3", 3, Stack1000),
    ];

    [TestMethod]
    public async Task LeaveSeat_BetweenHands_NextHandIsDealtWithoutTheSeat()
    {
        using var session = await LeaveSession.CreateAsync(_threeSeats);
        await session.PlayHandToEndAsync();

        var afterLeave = await session.ApplyOkAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, 2));

        afterLeave.HandData!.No.Should().Be(1);
        afterLeave.HandData.Seats.Select(seat => seat.SeatNo).Should().Equal(1, 2, 3);
        afterLeave.HandData.Seats.Single(seat => seat.SeatNo == 2).HasLeft.Should().BeTrue();
        afterLeave.HandData.Seats.Where(seat => seat.SeatNo != 2).Should().OnlyContain(seat => !seat.HasLeft);
        (await session.GetGameAsync()).Should().Be(afterLeave, "the leave is persisted, not only returned");

        var hand2 = await session.StartHandAsync();

        hand2.No.Should().Be(2);
        hand2.Seats.Select(seat => seat.SeatNo).Should().Equal(1, 3);
        hand2.Seats.Should().OnlyContain(seat => !seat.HasLeft);
        session.HoleCardRequests.Where(request => request.HandNo == 2).Select(request => request.SeatNo).Order()
            .Should().Equal(new byte[] { 1, 3 }, "the shuffler is asked for the seats of the new hand only");

        var players = await session.LoadPlayersAsync();
        players.Should().ContainSingle(player => player.SeatNo == 2)
            .Which.HandNoEnd.Should().Be(1, "the player's last hand is the one that was current when they left");
        players.Where(player => player.SeatNo != 2).Should().OnlyContain(player => player.HandNoEnd == null);
    }

    [TestMethod]
    public async Task LeaveSeat_DuringAHand_FailsWithPreviousHandNotEndedAndSavesNothing()
    {
        using var session = await LeaveSession.CreateAsync(_threeSeats);
        var before = await session.GetGameAsync();
        var rowsBefore = await session.CountRowsAsync();

        var result = await session.ApplyRawAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, 2));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.PreviousHandNotEnded);
        (await session.CountRowsAsync()).Should().Equal(rowsBefore);
        (await session.LoadPlayersAsync()).Should().OnlyContain(player => player.HandNoEnd == null);
        (await session.GetGameAsync()).Should().Be(before);
    }

    [TestMethod]
    [DataRow((byte)5, false, DisplayName = "Empty seat")]
    [DataRow((byte)0, false, DisplayName = "Seat 0")]
    [DataRow((byte)7, false, DisplayName = "Seat above MaxPlayersCount")]
    [DataRow((byte)2, true, DisplayName = "Seat already left")]
    public async Task LeaveSeat_SeatWithNobodyToLeave_FailsWithInvalidSeat(byte seatNo, bool leaveFirst)
    {
        using var session = await LeaveSession.CreateAsync(_threeSeats);
        await session.PlayHandToEndAsync();
        if (leaveFirst)
        {
            await session.ApplyOkAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, seatNo));
        }

        var before = await session.GetGameAsync();
        var playersBefore = await session.LoadPlayersAsync();

        var result = await session.ApplyRawAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, seatNo));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidSeat);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await session.LoadPlayersAsync()).Should().Equal(playersBefore);
        (await session.GetGameAsync()).Should().Be(before);
    }

    [TestMethod]
    public async Task LeaveSeat_BeforeStartGame_FailsWithGameNotStarted()
    {
        using var session = await LeaveSession.CreateUnstartedAsync();

        var result = await session.ApplyRawAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, 1));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameNotStarted);
        (await session.GetGameAsync()).Version.Should().Be(1, "nothing was saved");
    }

    [TestMethod]
    public async Task LeaveSeat_ThenJoin_NewPlayerTakesTheSeatWithOwnBuyIn()
    {
        using var session = await LeaveSession.CreateAsync(_threeSeats);
        await session.PlayHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, 2));

        var hand2 = await session.StartHandAsync([new("Player 9", 2, 500M)]);

        hand2.Seats.Select(seat => seat.SeatNo).Should().Equal(1, 2, 3);
        var seat2 = hand2.Seats.Single(seat => seat.SeatNo == 2);
        seat2.Name.Should().Be("Player 9");
        seat2.HasLeft.Should().BeFalse();
        seat2.StartHandStack.Should().Be(500M, "the new player's stack is their own buy-in, not the leaver's");
        seat2.Hole.Should().HaveCount(2);
        hand2.Seats.Should().NotContain(seat => seat.Name == "Player 2");

        await session.PlayHandToEndAsync();
        var finished = (await session.GetGameAsync()).HandData!;
        finished.Seats.Where(seat => seat.SeatNo == 2).Select(seat => seat.Name).Should().Equal("Player 9");

        var seat2Players = (await session.LoadPlayersAsync()).Where(player => player.SeatNo == 2);
        seat2Players.Select(player => (player.Name, player.HandNoStart, player.HandNoEnd))
            .Should().Equal(("Player 2", 1, (int?)1), ("Player 9", 2, (int?)null));
    }

    [TestMethod]
    public async Task LeaveSeat_BustedSeat_CanBeRetakenOnlyAfterTheLeave()
    {
        using var session = await LeaveSession.CreateAsync(
            [new("Player 1", 1, 100M), new("Player 2", 2, Stack1000)],
            new GameOptionData { TableFormat = HoldemTableFormat.HeadsUp, MaxPlayersCount = 2, InitialSmallBlind = SmallBlind, InitialDealerSeatNo = 1 });

        // Heads-up the button (seat 1) acts first pre-flop; its 7-2 loses to aces and busts.
        await session.ApplyOkAsync(HoldemAction.Play(LeaveSession.GameId, PlayerActionType.AllIn));
        var bust = await session.ApplyOkAsync(HoldemAction.Play(LeaveSession.GameId, PlayerActionType.Call));
        bust.HandData!.Street.Should().Be(Street.Finished);
        var busted = bust.HandData.Seats.Single(seat => seat.SeatNo == 1);
        (busted.StartHandStack + busted.HandResult!.Net).Should().Be(0M, "seat 1 is busted");

        PlayerJoinData[] retake = [new("Player 9", 1, Stack1000)];
        var beforeLeave = await session.ApplyRawAsync(HoldemAction.StartHand(LeaveSession.GameId, retake));
        beforeLeave.IsFailed.Should().BeTrue();
        beforeLeave.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidSeat, "a busted seat stays seated until it leaves");

        await session.ApplyOkAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, 1));
        var hand2 = await session.StartHandAsync(retake);

        hand2.No.Should().Be(2);
        hand2.Seats.Select(seat => (seat.SeatNo, seat.Name, seat.StartHandStack))
            .Should().Equal(((byte)1, "Player 9", Stack1000), ((byte)2, "Player 2", 1100M));
    }

    [TestMethod]
    public async Task LeaveSeat_DealerLeaves_ButtonMovesToTheNextOccupiedSeat()
    {
        using var session = await LeaveSession.CreateAsync([.. _threeSeats, new("Player 4", 4, Stack1000)]);
        var hand1 = (await session.GetGameAsync()).HandData!;
        hand1.DealerSeatNo.Should().Be(1);
        await session.PlayHandToEndAsync();

        await session.ApplyOkAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, 1));
        var hand2 = await session.StartHandAsync();

        hand2.Seats.Select(seat => seat.SeatNo).Should().Equal(2, 3, 4);
        hand2.DealerSeatNo.Should().Be(2, "the button moves to the next occupied seat clockwise of the departed one");
        hand2.SmallBlindSeatNo.Should().Be(3);
        hand2.BigBlindSeatNo.Should().Be(4);
    }

    [TestMethod]
    [DataRow((byte)5, (byte)6, DisplayName = "Anchor between seats")]
    [DataRow((byte)7, (byte)2, DisplayName = "Anchor past the last seat wraps")]
    [DataRow((byte)1, (byte)2, DisplayName = "Anchor before the first seat")]
    [DataRow((byte)4, (byte)6, DisplayName = "Anchor at the table is skipped")]
    public void HandTable_NextSeatAfter_StartsClockwiseOfTheAnchor(byte anchor, byte expected)
    {
        var table = new HandTable
        {
            SmallBlind = SmallBlind,
            Deck = CardDeck.Predetermined([]),
            Seats = [.. new byte[] { 2, 4, 6 }.Select(seatNo => new TableSeat { SeatNo = seatNo, Name = $"Player {seatNo}" })],
        };

        table.NextSeatAfter(anchor, _ => true).Should().Be(expected);
    }

    [TestMethod]
    public async Task LeaveSeat_DownToOneSeat_StartHandFailsUntilSomeoneJoins()
    {
        using var session = await LeaveSession.CreateAsync(_threeSeats);
        await session.PlayHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, 1));
        await session.ApplyOkAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, 2));
        var rowsBefore = await session.CountRowsAsync();

        var alone = await session.ApplyRawAsync(HoldemAction.StartHand(LeaveSession.GameId));

        alone.IsFailed.Should().BeTrue();
        alone.Errors.Single().Code.Should().Be(HoldemErrorCode.NotEnoughPlayers);
        (await session.CountRowsAsync()).Should().Equal(rowsBefore);

        var hand2 = await session.StartHandAsync([new("Player 9", 4, Stack1000)]);

        hand2.No.Should().Be(2);
        hand2.Seats.Select(seat => seat.SeatNo).Should().Equal(3, 4);
    }

    [TestMethod]
    public async Task LeaveSeat_AcrossHands_ConservesChips()
    {
        using var session = await LeaveSession.CreateAsync(_threeSeats);
        await session.PlayHandToEndAsync();

        var afterLeave = (await session.ApplyOkAsync(HoldemAction.LeaveSeat(LeaveSession.GameId, 3))).HandData!;
        var leaver = afterLeave.Seats.Single(seat => seat.SeatNo == 3);
        var leaverStack = leaver.StartHandStack + leaver.HandResult!.Net;
        leaverStack.Should().Be(1005M, "seat 3 won the blinds");

        var hand2 = await session.StartHandAsync([new("Player 9", 5, 700M)]);
        const decimal buyIns = (3 * Stack1000) + 700M;
        hand2.Seats.Sum(seat => seat.StartHandStack).Should().Be(buyIns - leaverStack, "the leaver's stack is not dealt into the next hand");

        await session.PlayHandToEndAsync();
        var finished = (await session.GetGameAsync()).HandData!;
        var seatedStacks = finished.Seats.Sum(seat => seat.StartHandStack + seat.HandResult!.Net);

        seatedStacks.Should().Be(buyIns - leaverStack);
        (await session.SumBuyInsAsync()).Should().Be(buyIns);
    }

    [TestMethod]
    public void HoldemActionNames_LeaveSeat_IsLeaveSeat()
        => HoldemActionNames.Of(HoldemAction.LeaveSeat(LeaveSession.GameId, 1)).Should().Be("LeaveSeat");

    private sealed class LeaveSession : IDisposable
    {
        internal static readonly Guid GameId = Guid.Parse("{53453453-4534-5345-3453-453453453453}");

        private static readonly GameOptionData _sixMax = new() { MaxPlayersCount = 6, InitialSmallBlind = SmallBlind, InitialDealerSeatNo = 1 };

        private static readonly Card[] _board = CardsFrom("4s", "9h", "qd", "3c", "8s");

        private readonly TestEngineHost.DisposableTestData _host;
        private readonly IHoldemTable _engine;
        private readonly IDbContextFactory<HoldemDbContext> _contextFactory;

        private LeaveSession()
        {
            _host = TestEngineHost.Setup(out _engine, out _contextFactory, shuffler: new TestCardShuffler(
                getHoleFunc: (handNo, seatNo) =>
                {
                    HoleCardRequests.Add((handNo, seatNo));
                    return seatNo == 1 ? CardsFrom("7c", "2d") : CardsFrom("ac", "ad");
                },
                getDeckFunc: _ => _board));
        }

        internal ConcurrentBag<(int HandNo, byte SeatNo)> HoleCardRequests { get; } = [];

        internal static async Task<LeaveSession> CreateAsync(PlayerJoinData[] seats, GameOptionData? options = null)
        {
            var session = new LeaveSession();
            await session.ApplyOkAsync(new HoldemActionCreateGame(GameId, options ?? _sixMax));
            await session.ApplyOkAsync(new HoldemActionStartGame(GameId, seats));
            return session;
        }

        internal static async Task<LeaveSession> CreateUnstartedAsync()
        {
            var session = new LeaveSession();
            await session.ApplyOkAsync(new HoldemActionCreateGame(GameId, _sixMax));
            return session;
        }

        internal async Task<HandData> StartHandAsync(PlayerJoinData[]? joinSeats = null)
            => (await ApplyOkAsync(HoldemAction.StartHand(GameId, joinSeats))).HandData!;

        /// <summary>Ends the current hand by folding every seat on the clock until one is left.</summary>
        internal async Task PlayHandToEndAsync()
        {
            var game = await GetGameAsync();
            while (game.HandData!.Street != Street.Finished)
            {
                game = await ApplyOkAsync(HoldemAction.Play(GameId, PlayerActionType.Fold));
            }
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

        internal async Task<List<(byte SeatNo, string Name, int HandNoStart, int? HandNoEnd)>> LoadPlayersAsync()
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var players = await dbContext.Set<Player>().AsNoTracking().ToListAsync();
            return [.. players
                .OrderBy(player => player.SeatNo).ThenBy(player => player.HandNoStart)
                .Select(player => (player.SeatNo, player.Name, player.HandNoStart, player.HandNoEnd))];
        }

        internal async Task<decimal> SumBuyInsAsync()
        {
            using var dbContext = _contextFactory.CreateDbContext();
            var amounts = await dbContext.Set<BuyIn>().Select(buyIn => buyIn.Amount).ToListAsync();
            return amounts.Sum();
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
