using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Joining a game between hands: a player sits down after the first hand ends and before the
/// second hand begins. The engine must accept the join, deal them in, and track button/blind positions correctly.
/// </summary>
[TestClass]
public sealed class SeatJoinTests
{
    private const decimal SmallBlind = 5M;
    private const decimal Stack1000 = 1000M;

    [TestMethod]
    public async Task JoinBetweenHands_EmptySeat_IsDealtIn()
    {
        using var session = await JoinSession.CreateAsync();

        var hand1 = await session.FirstHandAsync();
        hand1.Seats.Should().HaveCount(3, "three seats initially");
        hand1.Seats.Select(s => s.SeatNo).Should().Equal(1, 2, 3);

        await session.PlayHandToEndAsync(hand1.Seats.Count);

        var hand2 = await session.JoinAndStartHandAsync([new("Player 4", 5, 777M)]);
        hand2.No.Should().Be(2);
        hand2.Seats.Should().HaveCount(4, "three original plus one joined");
        hand2.Seats.Select(s => s.SeatNo).Should().Equal(1, 2, 3, 5);

        var seat5 = hand2.Seats.Single(s => s.SeatNo == 5);
        seat5.StartHandStack.Should().Be(777M, "the joined player's start hand stack is their buy-in");
        seat5.Hole.Should().HaveCount(2, "joined player is dealt two hole cards");
    }

    [TestMethod]
    public async Task JoinBetweenHands_ButtonAndBlinds_FollowTheRing()
    {
        using var session = await JoinSession.CreateAsync();

        var hand1 = await session.FirstHandAsync();
        byte hand1DealerSeatNo = hand1.DealerSeatNo;
        byte hand1SmallBlindSeatNo = hand1.SmallBlindSeatNo;
        byte hand1BigBlindSeatNo = hand1.BigBlindSeatNo;

        await session.PlayHandToEndAsync(hand1.Seats.Count);

        var hand2 = await session.JoinAndStartHandAsync([new("Player 4", 5, 777M)]);
        byte hand2DealerSeatNo = hand2.DealerSeatNo;
        byte hand2SmallBlindSeatNo = hand2.SmallBlindSeatNo;
        byte hand2BigBlindSeatNo = hand2.BigBlindSeatNo;

        // Button advances by one occupied seat, wrapping if needed
        byte[] occupiedSeatsHand2 = [1, 2, 3, 5];
        int currentButtonIndex = Array.IndexOf(occupiedSeatsHand2, hand1DealerSeatNo);
        int nextButtonIndex = (currentButtonIndex + 1) % occupiedSeatsHand2.Length;
        hand2DealerSeatNo.Should().Be(occupiedSeatsHand2[nextButtonIndex],
            "the button advances to the next occupied seat after the current dealer");

        await session.PlayHandToEndAsync(hand2.Seats.Count);

        var hand3 = await session.JoinAndStartHandAsync([]);
        byte hand3DealerSeatNo = hand3.DealerSeatNo;
        byte hand3SmallBlindSeatNo = hand3.SmallBlindSeatNo;
        byte hand3BigBlindSeatNo = hand3.BigBlindSeatNo;

        // Button advances again
        occupiedSeatsHand2 = [1, 2, 3, 5];
        currentButtonIndex = Array.IndexOf(occupiedSeatsHand2, hand2DealerSeatNo);
        nextButtonIndex = (currentButtonIndex + 1) % occupiedSeatsHand2.Length;
        hand3DealerSeatNo.Should().Be(occupiedSeatsHand2[nextButtonIndex],
            "button continues to advance through the second transition");
    }

    [TestMethod]
    public async Task JoinBetweenHands_ChipsAreConserved()
    {
        using var session = await JoinSession.CreateAsync();

        var hand1 = await session.FirstHandAsync();
        decimal hand1InitialChips = hand1.Seats.Sum(s => s.StartHandStack);
        await session.PlayHandToEndAsync(hand1.Seats.Count);

        var hand2 = await session.JoinAndStartHandAsync([new("Player 4", 5, 777M)]);
        decimal hand2InitialChips = hand2.Seats.Sum(s => s.StartHandStack);
        hand2InitialChips.Should().Be(hand1InitialChips + 777M, "join adds the new player's buy-in");

        await session.PlayHandToEndAsync(hand2.Seats.Count);

        var hand2EndGame = await session.GetGameAsync();
        decimal hand2FinalChips = hand2EndGame.HandData!.Seats.Sum(s => s.StartHandStack + s.HandResult!.Net);
        hand2FinalChips.Should().Be(hand2InitialChips, "chips are conserved within a hand");
    }

    [TestMethod]
    public async Task JoinBetweenHands_OccupiedSeat_FailsWithInvalidSeat()
    {
        using var session = await JoinSession.CreateAsync();

        var hand1 = await session.FirstHandAsync();
        await session.PlayHandToEndAsync(hand1.Seats.Count);

        var rowsBefore = await session.CountRowsAsync();

        var result = await session.ApplyRawAsync(
            new HoldemActionStartHand(JoinSession.GameId) { JoinSeats = [new("Player 4", 1, 500M)] });

        result.IsFailed.Should().BeTrue("joining an occupied seat should fail");
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.InvalidSeat);
        (await session.CountRowsAsync()).Should().Equal(rowsBefore, "no rows should be persisted on failure");
    }

    [TestMethod]
    public async Task JoinDuringHand_FailsWithPreviousHandNotEnded()
    {
        using var session = await JoinSession.CreateAsync();

        var hand1 = await session.FirstHandAsync();
        var rowsBefore = await session.CountRowsAsync();

        var result = await session.ApplyRawAsync(
            new HoldemActionStartHand(JoinSession.GameId) { JoinSeats = [new("Player 4", 5, 500M)] });

        result.IsFailed.Should().BeTrue("joining during a hand should fail");
        result.Errors.Single().Code.Should().Be(HoldemErrorCode.PreviousHandNotEnded);
        (await session.CountRowsAsync()).Should().Equal(rowsBefore, "no rows should be persisted on failure");
    }

    private sealed class JoinSession : IDisposable
    {
        internal static readonly Guid GameId = Guid.Parse("{55555555-5555-5555-5555-555555555555}");

        private static readonly GameOptionData _options = new() { MaxPlayersCount = 6, InitialSmallBlind = SmallBlind, InitialDealerSeatNo = 1 };

        private static readonly PlayerJoinData[] _initialSeats =
        [
            new("Player 1", 1, Stack1000),
            new("Player 2", 2, Stack1000),
            new("Player 3", 3, Stack1000),
        ];

        private static readonly Card[] _board = CardsFrom("4s", "9h", "qd", "3c", "8s");

        private readonly TestEngineHost.DisposableTestData _host;
        private readonly IHoldemTable _engine;
        private readonly IDbContextFactory<HoldemDbContext> _contextFactory;

        private JoinSession()
        {
            _host = TestEngineHost.Setup(out _engine, out _contextFactory, shuffler: new TestCardShuffler(
                getHoleFunc: (_, seatNo) => [Card.Parse("ac"), Card.Parse("kc")],
                getDeckFunc: _ => _board));
        }

        internal static async Task<JoinSession> CreateAsync()
        {
            var session = new JoinSession();
            await session.ApplyOkAsync(new HoldemActionCreateGame(GameId, _options));
            await session.ApplyOkAsync(new HoldemActionStartGame(GameId, _initialSeats));
            return session;
        }

        internal async Task<HandData> FirstHandAsync()
        {
            var gameData = await GetGameAsync();
            return gameData.HandData!;
        }

        internal async Task<HandData> JoinAndStartHandAsync(PlayerJoinData[] joinSeats)
        {
            var gameData = await ApplyOkAsync(new HoldemActionStartHand(GameId) { JoinSeats = joinSeats });
            return gameData.HandData!;
        }

        internal async Task PlayHandToEndAsync(int seatCount)
        {
            // End a hand by having all but one seated player fold
            int foldsNeeded = seatCount - 1;
            GameData result = null!;
            for (int i = 0; i < foldsNeeded; i++)
            {
                result = await ApplyOkAsync(new HoldemActionPlay(GameId, PlayerActionType.Fold, 0));
            }
            result.HandData!.Street.Should().Be(Street.Finished, "all but one player folded");
        }

        internal async Task<GameData> GetGameAsync()
        {
            return await ApplyOkAsync(new HoldemActionGetGame(GameId));
        }

        internal async Task<HoldemResult<GameData>> ApplyRawAsync(HoldemAction holdemAction)
        {
            return await _engine.ApplyAsync(holdemAction);
        }

        internal async Task<int[]> CountRowsAsync()
        {
            using var dbContext = _contextFactory.CreateDbContext();
            return
            [
                await dbContext.Set<Game>().CountAsync(),
                await dbContext.Set<Hand>().CountAsync(),
                await dbContext.Set<HandAction>().CountAsync(),
                await dbContext.Set<Player>().CountAsync(),
                await dbContext.Set<BuyIn>().CountAsync(),
            ];
        }

        private async Task<GameData> ApplyOkAsync(HoldemAction holdemAction)
        {
            var result = await _engine.ApplyAsync(holdemAction);

            result.IsSuccess.Should().BeTrue(
                $"{holdemAction.GetType().Name} should be accepted: " +
                string.Join("; ", result.Errors.Select(error => error.Message)));

            return result.Value;
        }

        private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];

        public void Dispose() => _host.Dispose();
    }
}
