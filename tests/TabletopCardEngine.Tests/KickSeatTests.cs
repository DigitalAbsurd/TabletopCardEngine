using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Diagnostics;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Removing a player with <see cref="HoldemActionKickSeat"/>: between hands it is a <c>LeaveSeat</c>, and during a hand
/// a seat still owing decisions is folded at once, on the clock or not, while the turn stays where it was.
/// </summary>
[TestClass]
public sealed class KickSeatTests
{
    private const decimal SmallBlind = 5M;
    private const decimal Stack1000 = 1000M;

    private static readonly PlayerJoinData[] _threeSeats = Seats(3);

    private static readonly PlayerJoinData[] _sixSeats = Seats(6);

    [TestMethod]
    public async Task KickSeat_BetweenHands_LeavesLikeLeaveSeatAndCancelsThePendingTopUp()
    {
        using var session = await KickSession.CreateAsync(_threeSeats, KickSession.SixMax with { AllowTopUp = true });
        await session.FoldHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.TopUp(KickSession.GameId, 2, 200M));
        var rowsBefore = await session.CountActionsAsync();

        var afterKick = (await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 2))).HandData!;

        var kicked = afterKick.Seats.Single(seat => seat.SeatNo == 2);
        kicked.HasLeft.Should().BeTrue();
        kicked.PendingBuyIn.Should().Be(0M, "the pending top-up leaves with the player");
        (await session.CountActionsAsync()).Should().Be(rowsBefore, "an ended hand gets no ledger row");
        (await session.SumBuyInsAsync()).Should().Be(3 * Stack1000, "the top-up row is cancelled");

        var hand2 = await session.StartHandAsync();

        hand2.Seats.Select(seat => seat.SeatNo).Should().Equal(1, 3);
        (await session.LoadPlayersAsync()).Single(player => player.SeatNo == 2).HandNoEnd.Should().Be(1);
    }

    [TestMethod]
    public async Task KickSeat_SeatOnTheClockPreFlop_RecordsTheFoldANormalFoldWould()
    {
        using var kicked = await KickSession.CreateAsync(_sixSeats);
        using var folded = await KickSession.CreateAsync(_sixSeats);
        var hand1 = (await kicked.GetGameAsync()).HandData!;
        hand1.NextPlayerSeatNo.Should().Be(4, "under the gun of a six-handed table with the button on seat 1");

        var afterKick = (await kicked.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 4))).HandData!;
        var afterFold = (await folded.ApplyOkAsync(HoldemAction.Play(KickSession.GameId, PlayerActionType.Fold))).HandData!;

        afterKick.Actions[^1].Should().Be(afterFold.Actions[^1], "the ledger row is the one Play would have written");
        afterKick.Actions[^1].SeatNo.Should().Be(4);
        afterKick.Actions[^1].ActionType.Should().Be(HandActionType.Fold);
        afterKick.NextPlayerSeatNo.Should().Be(afterFold.NextPlayerSeatNo).And.Be(5);
        afterKick.Evaluations.Should().Equal(afterFold.Evaluations);
        afterKick.Seats.Single(seat => seat.SeatNo == 4).HasLeft.Should().BeTrue();
    }

    [TestMethod]
    public async Task KickSeat_SeatsNotOnTheClock_FoldWithoutMovingTheTurnAndTheHandPlaysOn()
    {
        using var session = await KickSession.CreateAsync(_sixSeats);

        var preFlop = (await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 6))).HandData!;

        preFlop.Actions[^1].Should().Match<PlayerActionData>(action =>
            action.SeatNo == 6 && action.ActionType == HandActionType.Fold && action.Street == Street.PreFlop);
        preFlop.NextPlayerSeatNo.Should().Be(4, "seat 4 is still on the clock");
        preFlop.Street.Should().Be(Street.PreFlop);
        (await session.GetGameAsync()).HandData.Should().Be(preFlop, "the out-of-turn fold rehydrates to the same table");

        var actors = await session.PlayPassivelyUntilAsync(hand => hand.Street == Street.Flop);
        actors.Should().Equal(4, 5, 1, 2, 3);

        var flop = (await session.GetGameAsync()).HandData!;
        flop.NextPlayerSeatNo.Should().Be(2, "the small blind acts first after the flop");
        var onFlop = (await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 5))).HandData!;

        onFlop.Actions[^1].Should().Match<PlayerActionData>(action =>
            action.SeatNo == 5 && action.ActionType == HandActionType.Fold && action.Street == Street.Flop);
        onFlop.NextPlayerSeatNo.Should().Be(2);
        onFlop.Street.Should().Be(Street.Flop);
        onFlop.CommunityCards.Should().HaveCount(3);

        actors = await session.PlayPassivelyUntilAsync(hand => hand.Street == Street.Finished);
        actors.Should().Equal(2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 1);

        var finished = (await session.GetGameAsync()).HandData!;
        finished.CommunityCards.Should().HaveCount(5);
        finished.Seats.Single(seat => seat.SeatNo == 1).HandResult!.Net.Should().Be(40M, "seat 1's aces win the four big blinds of the other callers");
        finished.Seats.Sum(seat => seat.HandResult!.Net).Should().Be(0M);
        finished.Seats.Where(seat => seat.SeatNo is 5 or 6).Should().OnlyContain(seat => seat.HasLeft && seat.HandResult!.Description == null);

        var hand2 = await session.StartHandAsync();

        hand2.Seats.Select(seat => seat.SeatNo).Should().Equal(1, 2, 3, 4);
        session.HoleCardRequests(handNo: 2).Should().Equal(1, 2, 3, 4);
    }

    [TestMethod]
    public async Task KickSeat_HeadsUpSeatNotOnTheClock_EndsTheHandForTheOtherSeat()
    {
        using var session = await KickSession.CreateAsync(
            Seats(2),
            new GameOptionData { TableFormat = HoldemTableFormat.HeadsUp, MaxPlayersCount = 2, InitialSmallBlind = SmallBlind, InitialDealerSeatNo = 1 });
        var hand1 = (await session.GetGameAsync()).HandData!;
        hand1.NextPlayerSeatNo.Should().Be(1, "heads-up the button acts first pre-flop");

        var afterKick = (await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 2))).HandData!;

        afterKick.Street.Should().Be(Street.Finished);
        afterKick.NextPlayerSeatNo.Should().Be(0);
        afterKick.CommunityCards.Should().BeEmpty("a hand won by a fold deals no board");
        afterKick.Seats.Select(seat => (seat.SeatNo, seat.HandResult!.Net)).Should().Equal(((byte)1, 10M), ((byte)2, -10M));
        afterKick.Seats.Sum(seat => seat.HandResult!.Net).Should().Be(0M);
        afterKick.Seats.Single(seat => seat.SeatNo == 2).HasLeft.Should().BeTrue();
    }

    [TestMethod]
    public async Task KickSeat_AllInSeat_WritesNoRowAndKeepsItInThePotToShowdown()
    {
        using var session = await KickSession.CreateAsync([new("Player 1", 1, 100M), new("Player 2", 2, Stack1000), new("Player 3", 3, Stack1000)]);
        await session.ApplyOkAsync(HoldemAction.Play(KickSession.GameId, PlayerActionType.AllIn));
        var rowsBefore = await session.CountActionsAsync();

        var afterKick = (await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 1))).HandData!;

        (await session.CountActionsAsync()).Should().Be(rowsBefore, "an all-in seat has nothing left to fold");
        afterKick.NextPlayerSeatNo.Should().Be(2);
        afterKick.Seats.Single(seat => seat.SeatNo == 1).Should().Match<SeatData>(seat => seat.HasLeft && seat.IsAllIn);

        await session.PlayPassivelyUntilAsync(hand => hand.Street == Street.Finished);

        var finished = (await session.GetGameAsync()).HandData!;
        var seat1 = finished.Seats.Single(seat => seat.SeatNo == 1);
        seat1.HandResult!.Net.Should().Be(200M, "the aces win the main pot of three 100s");
        seat1.HandResult.Description.Should().NotBeNull("the seat reached showdown");
        finished.Seats.Sum(seat => seat.HandResult!.Net).Should().Be(0M);

        var hand2 = await session.StartHandAsync();
        hand2.Seats.Select(seat => seat.SeatNo).Should().Equal(2, 3);
    }

    [TestMethod]
    public async Task KickSeat_SeatWithTheMostIn_LeavesItsChipsToTheSeatStillContesting()
    {
        using var session = await KickSession.CreateAsync([new("Player 1", 1, 100M), new("Player 2", 2, Stack1000), new("Player 3", 3, Stack1000)]);
        await session.ApplyOkAsync(HoldemAction.Play(KickSession.GameId, PlayerActionType.AllIn));
        await session.ApplyOkAsync(HoldemAction.Play(KickSession.GameId, PlayerActionType.Raise, 500M));

        // Seat 2 has the most in and is not on the clock; seat 3 then folds, leaving the all-in seat alone.
        var afterKick = (await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 2))).HandData!;
        afterKick.NextPlayerSeatNo.Should().Be(3);
        var finished = (await session.ApplyOkAsync(HoldemAction.Play(KickSession.GameId, PlayerActionType.Fold))).HandData!;

        finished.Street.Should().Be(Street.Finished);
        finished.Seats.Select(seat => (seat.SeatNo, seat.HandResult!.Net))
            .Should().Equal([((byte)1, 510M), ((byte)2, -500M), ((byte)3, -10M)], "a folded seat's chips are dead money for the seat still in");
        finished.Seats.Sum(seat => seat.HandResult!.Net).Should().Be(0M);
    }

    [TestMethod]
    public async Task KickSeat_FoldedSeat_WritesNoRow()
    {
        using var session = await KickSession.CreateAsync(_threeSeats);
        await session.ApplyOkAsync(HoldemAction.Play(KickSession.GameId, PlayerActionType.Fold));
        var before = (await session.GetGameAsync()).HandData!;
        var rowsBefore = await session.CountActionsAsync();

        var afterKick = (await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 1))).HandData!;

        (await session.CountActionsAsync()).Should().Be(rowsBefore);
        afterKick.NextPlayerSeatNo.Should().Be(before.NextPlayerSeatNo);
        afterKick.Seats.Single(seat => seat.SeatNo == 1).HasLeft.Should().BeTrue();
    }

    [TestMethod]
    public async Task KickSeat_Dealer_NextButtonIsTheNextOccupiedSeat()
    {
        using var session = await KickSession.CreateAsync(Seats(4));
        var hand1 = (await session.GetGameAsync()).HandData!;
        hand1.DealerSeatNo.Should().Be(1);
        hand1.NextPlayerSeatNo.Should().Be(4);

        var afterKick = (await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 1))).HandData!;
        afterKick.NextPlayerSeatNo.Should().Be(4);
        await session.FoldHandToEndAsync();

        var hand2 = await session.StartHandAsync();

        hand2.Seats.Select(seat => seat.SeatNo).Should().Equal(2, 3, 4);
        hand2.DealerSeatNo.Should().Be(2, "the button moves to the next occupied seat clockwise of the departed one");
        hand2.SmallBlindSeatNo.Should().Be(3);
        hand2.BigBlindSeatNo.Should().Be(4);
    }

    [TestMethod]
    [DataRow((byte)5, false, false, DisplayName = "Empty seat")]
    [DataRow((byte)0, false, false, DisplayName = "Seat 0")]
    [DataRow((byte)7, false, false, DisplayName = "Seat above MaxPlayersCount")]
    [DataRow((byte)2, true, false, DisplayName = "Kicked twice during the hand")]
    [DataRow((byte)2, true, true, DisplayName = "Kicked twice, the second between hands")]
    public async Task KickSeat_SeatWithNobodyToRemove_FailsWithInvalidSeat(byte seatNo, bool kickFirst, bool endHandFirst)
    {
        using var session = await KickSession.CreateAsync(_threeSeats);
        if (kickFirst)
        {
            await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, seatNo));
        }
        if (endHandFirst)
        {
            await session.FoldHandToEndAsync();
        }

        var before = await session.GetGameAsync();
        var rowsBefore = await session.CountActionsAsync();

        var result = await session.ApplyRawAsync(HoldemAction.KickSeat(KickSession.GameId, seatNo));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidSeat);
        result.Errors.Single().Message.Should().NotBeNullOrWhiteSpace();
        (await session.CountActionsAsync()).Should().Be(rowsBefore);
        (await session.GetGameAsync()).Should().Be(before);
    }

    [TestMethod]
    public async Task LeaveSeat_AfterAKick_FailsWithInvalidSeat()
    {
        using var session = await KickSession.CreateAsync(_threeSeats);
        await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 2));
        await session.FoldHandToEndAsync();

        var result = await session.ApplyRawAsync(HoldemAction.LeaveSeat(KickSession.GameId, 2));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidSeat);
    }

    [TestMethod]
    public async Task KickSeat_BeforeStartGame_FailsWithGameNotStarted()
    {
        using var session = await KickSession.CreateUnstartedAsync();

        var result = await session.ApplyRawAsync(HoldemAction.KickSeat(KickSession.GameId, 1));

        result.IsFailed.Should().BeTrue();
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.GameNotStarted);
        (await session.GetGameAsync()).Version.Should().Be(1, "nothing was saved");
    }

    [TestMethod]
    public async Task KickSeat_ThenJoin_NewPlayerTakesTheSeat()
    {
        using var session = await KickSession.CreateAsync(_threeSeats);
        await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 3));
        await session.FoldHandToEndAsync();

        var hand2 = await session.StartHandAsync([new("Player 9", 3, 500M)]);

        hand2.Seats.Select(seat => (seat.SeatNo, seat.Name, seat.StartHandStack, seat.HasLeft))
            .Should().Equal(((byte)1, "Player 1", Stack1000, false), ((byte)2, "Player 2", 1010M, false), ((byte)3, "Player 9", 500M, false));
        var seat3Players = (await session.LoadPlayersAsync()).Where(player => player.SeatNo == 3);
        seat3Players.Select(player => (player.Name, player.HandNoStart, player.HandNoEnd))
            .Should().Equal(("Player 3", 1, (int?)1), ("Player 9", 2, (int?)null));
    }

    [TestMethod]
    public async Task KickSeat_OverAGame_ConservesChipsKickedStacksIncluded()
    {
        using var session = await KickSession.CreateAsync(_sixSeats);
        var leftWith = new Dictionary<string, decimal>();

        // Hand 1: one seat out of turn, one on the clock, then played down to showdown.
        await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 6));
        await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 4));
        await session.PlayPassivelyUntilAsync(hand => hand.Street == Street.Finished);
        await CollectLeaversAsync();

        // Hand 2: a raise, then the raiser is removed out of turn, and the rest fold to the big blind.
        await session.StartHandAsync([new("Player 7", 6, 300M)]);
        var hand2 = (await session.GetGameAsync()).HandData!;
        var raiser = hand2.NextPlayerSeatNo;
        await session.ApplyOkAsync(HoldemAction.Play(KickSession.GameId, PlayerActionType.Raise, 30M));
        await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, raiser));
        await session.FoldHandToEndAsync();
        await CollectLeaversAsync();

        // Hand 3: between hands, then played to the end.
        await session.ApplyOkAsync(HoldemAction.KickSeat(KickSession.GameId, 1));
        await CollectLeaversAsync();
        await session.StartHandAsync();
        await session.PlayPassivelyUntilAsync(hand => hand.Street == Street.Finished);

        var finished = (await session.GetGameAsync()).HandData!;
        var seated = finished.Seats.Where(seat => !seat.HasLeft).Sum(seat => seat.StartHandStack + seat.HandResult!.Net);
        raiser.Should().Be(6, "the new player under the gun raises");
        leftWith.Keys.Should().BeEquivalentTo("Player 6", "Player 4", "Player 7", "Player 1");
        (seated + leftWith.Values.Sum()).Should().Be(await session.SumBuyInsAsync());
        (await session.SumBuyInsAsync()).Should().Be((6 * Stack1000) + 300M);

        async Task CollectLeaversAsync()
        {
            foreach (var seat in (await session.GetGameAsync()).HandData!.Seats.Where(seat => seat.HasLeft))
            {
                leftWith.TryAdd(seat.Name, seat.StartHandStack + seat.HandResult!.Net);
            }
        }
    }

    [TestMethod]
    public async Task KickSeat_ProbeThreeSeats_TheSeatOnTheClockStaysAndPlays()
    {
        using var session = await KickSession.CreateAsync(_threeSeats);
        var before = (await session.GetGameAsync()).HandData!;
        before.NextPlayerSeatNo.Should().Be(1);

        var afterKick = await session.ApplyRawAsync(HoldemAction.KickSeat(KickSession.GameId, 2));

        afterKick.IsSuccess.Should().BeTrue();
        afterKick.Value.HandData!.NextPlayerSeatNo.Should().Be(1);
        (await session.ApplyRawAsync(HoldemAction.Play(KickSession.GameId, PlayerActionType.Call))).IsSuccess.Should().BeTrue();
    }

    [TestMethod]
    public void HoldemActionNames_KickSeat_IsKickSeat()
        => HoldemActionNames.Of(HoldemAction.KickSeat(KickSession.GameId, 1)).Should().Be("KickSeat");

    private static PlayerJoinData[] Seats(int count)
        => [.. Enumerable.Range(1, count).Select(seatNo => new PlayerJoinData($"Player {seatNo}", (byte)seatNo, Stack1000))];

    private sealed class KickSession : IDisposable
    {
        internal static readonly Guid GameId = Guid.Parse("{6a1c6a1c-6a1c-6a1c-6a1c-6a1c6a1c6a1c}");

        internal static readonly GameOptionData SixMax = new() { MaxPlayersCount = 6, InitialSmallBlind = SmallBlind, InitialDealerSeatNo = 1 };

        private static readonly Card[] _board = CardsFrom("4s", "9h", "qd", "3c", "8s");

        // Seat 1's aces beat every other pocket pair, and no two seats share a card with each other or the board.
        private static readonly Dictionary<byte, Card[]> _holes = new()
        {
            [1] = CardsFrom("ac", "ad"),
            [2] = CardsFrom("kc", "kd"),
            [3] = CardsFrom("7c", "2d"),
            [4] = CardsFrom("jc", "jd"),
            [5] = CardsFrom("tc", "td"),
            [6] = CardsFrom("5c", "5d"),
        };

        private readonly TestEngineHost.DisposableTestData _host;
        private readonly IHoldemTable _engine;
        private readonly IDbContextFactory<HoldemDbContext> _contextFactory;
        private readonly List<(int HandNo, byte SeatNo)> _holeCardRequests = [];

        private KickSession()
        {
            _host = TestEngineHost.Setup(out _engine, out _contextFactory, shuffler: new TestCardShuffler(
                getHoleFunc: (handNo, seatNo) =>
                {
                    lock (_holeCardRequests)
                    {
                        _holeCardRequests.Add((handNo, seatNo));
                    }
                    return _holes[seatNo];
                },
                getDeckFunc: _ => _board));
        }

        internal static async Task<KickSession> CreateAsync(PlayerJoinData[] seats, GameOptionData? options = null)
        {
            var session = new KickSession();
            await session.ApplyOkAsync(new HoldemActionCreateGame(GameId, options ?? SixMax));
            await session.ApplyOkAsync(new HoldemActionStartGame(GameId, seats));
            return session;
        }

        internal static async Task<KickSession> CreateUnstartedAsync()
        {
            var session = new KickSession();
            await session.ApplyOkAsync(new HoldemActionCreateGame(GameId, SixMax));
            return session;
        }

        internal IEnumerable<byte> HoleCardRequests(int handNo)
        {
            lock (_holeCardRequests)
            {
                return [.. _holeCardRequests.Where(request => request.HandNo == handNo).Select(request => request.SeatNo).Order()];
            }
        }

        internal async Task<HandData> StartHandAsync(PlayerJoinData[]? joinSeats = null)
            => (await ApplyOkAsync(HoldemAction.StartHand(GameId, joinSeats))).HandData!;

        /// <summary>Ends the current hand by folding every seat on the clock until one is left.</summary>
        internal async Task FoldHandToEndAsync()
        {
            var game = await GetGameAsync();
            while (game.HandData!.Street != Street.Finished)
            {
                game = await ApplyOkAsync(HoldemAction.Play(GameId, PlayerActionType.Fold));
            }
        }

        /// <summary>Checks, or calls when a check is not offered, until <paramref name="until"/> holds; returns who acted.</summary>
        internal async Task<List<byte>> PlayPassivelyUntilAsync(Func<HandData, bool> until)
        {
            var actors = new List<byte>();
            var hand = (await GetGameAsync()).HandData!;
            while (!until(hand))
            {
                actors.Add(hand.NextPlayerSeatNo);
                var action = hand.Evaluations.Any(evaluation => evaluation.PlayerActionType == PlayerActionType.Check)
                    ? PlayerActionType.Check
                    : PlayerActionType.Call;
                hand = (await ApplyOkAsync(HoldemAction.Play(GameId, action))).HandData!;
            }
            return actors;
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

        internal async Task<int> CountActionsAsync()
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return await dbContext.Set<HandAction>().CountAsync();
        }

        private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];

        public void Dispose() => _host.Dispose();
    }
}
