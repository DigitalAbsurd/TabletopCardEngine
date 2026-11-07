using FluentAssertions;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Engine;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Raising the blinds by <see cref="GameOptionData.BlindSchedule"/> when a hand is dealt: first the pure
/// <see cref="BlindSchedule.Next"/>, trigger by trigger, then the same rules through the engine on a scripted deck.
/// </summary>
[TestClass]
public sealed class BlindScheduleTests
{
    private const decimal InitialSmallBlind = 1M;

    private static readonly BlindLevelState _hand1 = new(InitialSmallBlind, 0, 1, 0);

    [TestMethod]
    public void BlindSchedule_NoSchedule_DealsEveryHandAtTheInitialLevel()
        => BlindSchedule.Next(null, InitialSmallBlind, new BlindLevelState(InitialSmallBlind, 0, 4, 0), 6, 5, 2)
            .Should().Be(new BlindLevelState(InitialSmallBlind, 0, 5, 0));

    [TestMethod]
    public void BlindSchedule_EveryNHands_RaisesOnceTheLevelIsNHandsOld()
    {
        var schedule = new BlindScheduleData { IncreaseEveryHands = 3 };

        Next(schedule, _hand1, 6, 3, 6).Should().Be(_hand1, "hand 3 is only two hands after the level began");
        var raised = Next(schedule, _hand1, 6, 4, 6);

        raised.Should().Be(new BlindLevelState(2M, 1, 4, 0));
        Next(schedule, raised, 6, 6, 6).Should().Be(raised, "the count restarts at the hand that raised");
        Next(schedule, raised, 6, 7, 6).Should().Be(new BlindLevelState(4M, 2, 7, 0));
    }

    [TestMethod]
    public void BlindSchedule_Thresholds_EachFiresWhenThePlayersFallToIt()
    {
        var schedule = new BlindScheduleData { PlayerThresholds = [3, 5] };

        var at5 = Next(schedule, _hand1, 6, 2, 5);
        at5.Should().Be(new BlindLevelState(2M, 1, 2, 1));

        var at4 = Next(schedule, at5, 5, 3, 4);
        at4.Should().Be(at5, "4 players pass no new threshold");

        Next(schedule, at4, 4, 4, 3).Should().Be(new BlindLevelState(4M, 2, 4, 2));
    }

    [TestMethod]
    public void BlindSchedule_Elimination_FiresOnlyWhenFewerPlayersAreDealtIn()
    {
        var schedule = new BlindScheduleData { IncreaseOnElimination = true };

        Next(schedule, _hand1, 6, 2, 6).Should().Be(_hand1);
        Next(schedule, _hand1, 5, 2, 6).Should().Be(_hand1, "a player joining never raises");
        Next(schedule, _hand1, 6, 2, 5).Should().Be(new BlindLevelState(2M, 1, 2, 0));
    }

    [TestMethod]
    public void BlindSchedule_TwoTriggersInTheSameHand_RaiseOneLevel()
    {
        var schedule = new BlindScheduleData { IncreaseEveryHands = 1, IncreaseOnElimination = true };

        Next(schedule, _hand1, 6, 2, 5).Should().Be(new BlindLevelState(2M, 1, 2, 0));
    }

    [TestMethod]
    public void BlindSchedule_ThreeTriggersInTheSameHand_RaiseOneLevel()
    {
        var schedule = new BlindScheduleData { IncreaseEveryHands = 1, IncreaseOnElimination = true, PlayerThresholds = [5, 4] };

        Next(schedule, _hand1, 6, 2, 4).Should().Be(new BlindLevelState(2M, 1, 2, 2), "both thresholds pass, still one level");
    }

    [TestMethod]
    public void BlindSchedule_ThresholdsMetAtHandOne_ArePassedWithoutFiring()
    {
        var schedule = new BlindScheduleData { PlayerThresholds = [2, 5, 3] };

        var hand1 = BlindSchedule.Next(schedule, InitialSmallBlind, default, 0, 1, 3);
        hand1.Should().Be(new BlindLevelState(InitialSmallBlind, 0, 1, 2), "5 and 3 are already met by three players");

        var hand2 = Next(schedule, hand1, 3, 2, 3);
        hand2.Should().Be(hand1);

        Next(schedule, hand2, 3, 3, 2).Should().Be(new BlindLevelState(2M, 1, 3, 3));
    }

    [TestMethod]
    public void BlindSchedule_Threshold_FiresOnceEvenWhenThePlayersRiseAndFallAgain()
    {
        var schedule = new BlindScheduleData { PlayerThresholds = [4] };

        var hand2 = Next(schedule, _hand1, 6, 2, 4);
        hand2.Should().Be(new BlindLevelState(2M, 1, 2, 1));

        var hand3 = Next(schedule, hand2, 4, 3, 5);
        hand3.Should().Be(hand2, "a join never raises");

        Next(schedule, hand3, 5, 4, 4).Should().Be(hand2, "the threshold already fired");
    }

    [TestMethod]
    public void BlindSchedule_Multiplier_RoundsToTheCentAwayFromZero()
    {
        var schedule = new BlindScheduleData { IncreaseEveryHands = 1, Multiplier = 1.5M };

        BlindSchedule.Next(schedule, 0.05M, new BlindLevelState(0.05M, 0, 1, 0), 2, 2, 2).SmallBlind.Should().Be(0.08M);
    }

    [TestMethod]
    public void BlindSchedule_Multiplier_StopsAtTheCap()
    {
        var schedule = new BlindScheduleData { IncreaseEveryHands = 1, MaxSmallBlind = 3M };

        var hand2 = Next(schedule, _hand1, 2, 2, 2);
        var hand3 = Next(schedule, hand2, 2, 3, 2);
        var hand4 = Next(schedule, hand3, 2, 4, 2);

        new[] { hand2, hand3, hand4 }.Select(state => (state.SmallBlind, state.BlindLevel))
            .Should().Equal((2M, 1), (3M, 2), (3M, 3));
    }

    [TestMethod]
    public void BlindSchedule_Levels_HoldTheLastOnceExhausted()
    {
        var schedule = new BlindScheduleData { IncreaseEveryHands = 1, Mode = BlindIncreaseMode.Levels, SmallBlindLevels = [2M, 5M] };

        var hand2 = Next(schedule, _hand1, 2, 2, 2);
        var hand3 = Next(schedule, hand2, 2, 3, 2);
        var hand4 = Next(schedule, hand3, 2, 4, 2);

        new[] { hand2, hand3, hand4 }.Select(state => (state.SmallBlind, state.BlindLevel))
            .Should().Equal((2M, 1), (5M, 2), (5M, 3));
    }

    [TestMethod]
    public async Task BlindSchedule_EveryTwoHands_RaisesTheHandBlindsTheRowsAndTheMinimumRaise()
    {
        using var session = await BlindSession.CreateAsync(3, new BlindScheduleData { IncreaseEveryHands = 2 });
        var hands = new List<HandData> { (await session.GetGameAsync()).HandData! };
        for (var handNo = 2; handNo <= 4; handNo++)
        {
            await session.FoldHandToEndAsync();
            hands.Add(await session.StartHandAsync());
        }

        hands.Select(hand => (hand.SmallBlind, hand.BigBlind, hand.BlindLevel))
            .Should().Equal((1M, 2M, 0), (1M, 2M, 0), (2M, 4M, 1), (2M, 4M, 1));
        foreach (var hand in hands)
        {
            hand.Actions.Single(action => action.ActionType == HandActionType.SmallBlind).Size.Should().Be(hand.SmallBlind);
            hand.Actions.Single(action => action.ActionType == HandActionType.BigBlind).Size.Should().Be(hand.BigBlind);
        }
        hands.Select(hand => hand.GetEvalMinValue(PlayerActionType.Raise))
            .Should().Equal([3M, 3M, 6M, 6M], "the pre-flop minimum raise is the big blind plus its own increment, the small blind");
    }

    [TestMethod]
    public async Task BlindSchedule_ShortStackAtTheHigherBlind_PostsAllIn()
    {
        var options = new GameOptionData
        {
            TableFormat = HoldemTableFormat.HeadsUp,
            MaxPlayersCount = 2,
            InitialSmallBlind = InitialSmallBlind,
            InitialDealerSeatNo = 1,
            BlindSchedule = new BlindScheduleData { IncreaseEveryHands = 2, Multiplier = 10M },
        };
        using var session = await BlindSession.CreateAsync([new("Player 1", 1, 1000M), new("Player 2", 2, 15M)], options);

        // Each button folds its hand, so seat 2 starts hand 3 in the big blind with its 15 again, short of the new big blind of 20.
        await session.FoldHandToEndAsync();
        await session.StartHandAsync();
        await session.FoldHandToEndAsync();
        var hand3 = await session.StartHandAsync();

        (hand3.SmallBlind, hand3.BigBlind, hand3.BlindLevel).Should().Be((10M, 20M, 1));
        hand3.BigBlindSeatNo.Should().Be(2);
        var post = hand3.Actions.Single(action => action.ActionType == HandActionType.BigBlind);
        (post.SeatNo, post.Size, post.Amount).Should().Be(((byte)2, 20M, 15M), "the post is capped at the stack");
        hand3.Seats.Single(seat => seat.SeatNo == 2).IsAllIn.Should().BeTrue();
        hand3.NextPlayerSeatNo.Should().Be(1, "the button still owes the short big blind a decision");
    }

    [TestMethod]
    public async Task BlindSchedule_LeaveSeatAndKickSeat_CountAsEliminations()
    {
        using var session = await BlindSession.CreateAsync(4, new BlindScheduleData { IncreaseOnElimination = true });
        await session.FoldHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.LeaveSeat(BlindSession.GameId, 4));
        var hand2 = await session.StartHandAsync();

        await session.FoldHandToEndAsync();
        var hand3 = await session.StartHandAsync();

        await session.ApplyOkAsync(HoldemAction.KickSeat(BlindSession.GameId, 3));
        await session.FoldHandToEndAsync();
        var hand4 = await session.StartHandAsync();

        new[] { hand2, hand3, hand4 }.Select(hand => (hand.Seats.Count, hand.SmallBlind, hand.BlindLevel))
            .Should().Equal((3, 2M, 1), (3, 2M, 1), (2, 4M, 2));
    }

    [TestMethod]
    public async Task BlindSchedule_GameWithoutASchedule_KeepsTheInitialBlinds()
    {
        using var session = await BlindSession.CreateAsync(3, null);
        var hands = new List<HandData> { (await session.GetGameAsync()).HandData! };
        await session.FoldHandToEndAsync();
        await session.ApplyOkAsync(HoldemAction.LeaveSeat(BlindSession.GameId, 3));
        hands.Add(await session.StartHandAsync());
        await session.FoldHandToEndAsync();
        hands.Add(await session.StartHandAsync());

        hands.Select(hand => (hand.SmallBlind, hand.BigBlind, hand.BlindLevel))
            .Should().Equal((1M, 2M, 0), (1M, 2M, 0), (1M, 2M, 0));
    }

    [TestMethod]
    public async Task BlindSchedule_Probe_IncreaseEveryHandOnTwoSeats_DealsHandTwoAtLevelOne()
    {
        using var session = await BlindSession.CreateAsync(2, new BlindScheduleData { IncreaseEveryHands = 1 });
        await session.FoldHandToEndAsync();

        var hand2 = await session.StartHandAsync();

        (hand2.SmallBlind, hand2.BigBlind, hand2.BlindLevel).Should().Be((2M, 4M, 1));
    }

    private static BlindLevelState Next(BlindScheduleData schedule, BlindLevelState previous, int previousDealtSeats, int handNo, int dealtSeats)
        => BlindSchedule.Next(schedule, InitialSmallBlind, previous, previousDealtSeats, handNo, dealtSeats);

    private sealed class BlindSession : IDisposable
    {
        internal static readonly Guid GameId = Guid.Parse("{b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1}");

        private static readonly Card[] _board = CardsFrom("4s", "9h", "qd", "3c", "8s");

        private static readonly Dictionary<byte, Card[]> _holes = new()
        {
            [1] = CardsFrom("ac", "ad"),
            [2] = CardsFrom("kc", "kd"),
            [3] = CardsFrom("7c", "2d"),
            [4] = CardsFrom("jc", "jd"),
        };

        private readonly TestEngineHost.DisposableTestData _host;
        private readonly IHoldemTable _engine;

        private BlindSession()
            => _host = TestEngineHost.Setup(out _engine, out _, shuffler: new TestCardShuffler(
                getHoleFunc: (_, seatNo) => _holes[seatNo],
                getDeckFunc: _ => _board));

        internal static Task<BlindSession> CreateAsync(int seatCount, BlindScheduleData? schedule)
            => CreateAsync(
                [.. Enumerable.Range(1, seatCount).Select(seatNo => new PlayerJoinData($"Player {seatNo}", (byte)seatNo, 1000M))],
                new GameOptionData { MaxPlayersCount = 6, InitialSmallBlind = InitialSmallBlind, InitialDealerSeatNo = 1, BlindSchedule = schedule });

        internal static async Task<BlindSession> CreateAsync(PlayerJoinData[] seats, GameOptionData options)
        {
            var session = new BlindSession();
            await session.ApplyOkAsync(new HoldemActionCreateGame(GameId, options));
            await session.ApplyOkAsync(new HoldemActionStartGame(GameId, seats));
            return session;
        }

        internal async Task<HandData> StartHandAsync()
            => (await ApplyOkAsync(HoldemAction.StartHand(GameId))).HandData!;

        /// <summary>Ends the current hand by folding every seat on the clock until one is left.</summary>
        internal async Task FoldHandToEndAsync()
        {
            var game = await GetGameAsync();
            while (game.HandData!.Street != Street.Finished)
            {
                game = await ApplyOkAsync(HoldemAction.Play(GameId, PlayerActionType.Fold));
            }
        }

        internal Task<GameData> GetGameAsync() => ApplyOkAsync(HoldemAction.GetGame(GameId));

        internal async Task<GameData> ApplyOkAsync(HoldemAction holdemAction)
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
