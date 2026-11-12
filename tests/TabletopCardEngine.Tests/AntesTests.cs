using FluentAssertions;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Classic and big blind antes on a scripted deck, with exact amounts. The ante is dead money: it is in
/// <see cref="HandData.Pot"/> from the deal on and in each seat's total contribution (so in the side pots), but
/// never in <see cref="HandData.StreetPot"/>, a seat's <see cref="SeatData.BetSize"/> or anything a call or a raise
/// is measured against. Every case runs at blinds 1/2.
/// </summary>
[TestClass]
public sealed class AntesTests
{
    private const decimal Stack = 100M;

    /// <summary>Seat 1 holds aces, which win every showdown on this board; nobody else pairs above a nine.</summary>
    private static readonly Dictionary<byte, Card[]> _holes = new()
    {
        [1] = CardsFrom("ah", "ad"),
        [2] = CardsFrom("7c", "2d"),
        [3] = CardsFrom("8c", "3d"),
        [4] = CardsFrom("9c", "4d"),
        [5] = CardsFrom("tc", "5d"),
        [6] = CardsFrom("jc", "6d"),
    };

    private static readonly Card[] _board = CardsFrom("ks", "qh", "2s", "5h", "9s");

    [TestMethod]
    public async Task ClassicAnte_SixHanded_AntesAreDeadMoneyInThePot()
    {
        using var session = await AnteSession.CreateAsync(new GameOptionData { InitialDealerSeatNo = 6, Ante = 1, AnteType = AnteType.Classic },
            [Stack, Stack, Stack, Stack, Stack, Stack]);

        var hand = (await session.StartAsync()).HandData!;

        hand.Ante.Should().Be(1M);
        hand.Actions.Select(action => (action.No, action.SeatNo, action.ActionType, action.Size, action.Amount)).Should().Equal(
            (1, (byte)1, HandActionType.Ante, 1M, 1M),
            (2, (byte)2, HandActionType.Ante, 1M, 1M),
            (3, (byte)3, HandActionType.Ante, 1M, 1M),
            (4, (byte)4, HandActionType.Ante, 1M, 1M),
            (5, (byte)5, HandActionType.Ante, 1M, 1M),
            (6, (byte)6, HandActionType.Ante, 1M, 1M),
            (7, (byte)1, HandActionType.SmallBlind, 1M, 1M),
            (8, (byte)2, HandActionType.BigBlind, 2M, 2M));
        hand.Actions.Where(action => action.ActionType == HandActionType.Ante).Should().OnlyContain(action => action.Street == Street.PreFlop);
        Call(hand).Should().Be(2M, "the ante does not count towards the call");
        RaiseWindow(hand).Min.Should().Be(4M, "the ante does not move the minimum raise");
        hand.Pot.Should().Be(6M, "the six antes are in the pot from the deal on");
        hand.StreetPot.Should().Be(3M, "the street holds the two blinds only");
        hand.SmallBlindSeatNo.Should().Be(1);
        hand.BigBlindSeatNo.Should().Be(2);
        hand.NextPlayerSeatNo.Should().Be(3);
        Seat(hand, 3).BetSize.Should().BeNull("an ante is not a street wager");
        Seat(hand, 3).LastAction.Should().BeNull("an ante is not a player's last action");
        Seat(hand, 3).Stack.Should().Be(Stack - 1);
        Seat(hand, 1).LastAction!.ActionType.Should().Be(HandActionType.SmallBlind);
        Seat(hand, 1).BetSize.Should().Be(1M);
        Seat(hand, 2).Stack.Should().Be(Stack - 3);

        foreach (var _ in Enumerable.Range(0, 5))
        {
            hand = (await session.PlayAsync(PlayerActionType.Fold)).HandData!;
        }

        hand.Street.Should().Be(Street.Finished);
        hand.Pot.Should().Be(9M);
        hand.StreetPot.Should().Be(0M);
        Seat(hand, 2).HandResult!.Net.Should().Be(6M, "the big blind wins the six antes and the three of the blinds, three of it its own");
        Seat(hand, 1).HandResult!.Net.Should().Be(-2M);
        new byte[] { 3, 4, 5, 6 }.Select(seatNo => Seat(hand, seatNo).HandResult!.Net).Should().OnlyContain(net => net == -1M);
        hand.Seats.Sum(seat => seat.HandResult!.Net).Should().Be(0M);
    }

    [TestMethod]
    public async Task BigBlindAnte_SixHanded_PostedAfterTheBigBlindAndTheOptionIsUnchanged()
    {
        using var session = await AnteSession.CreateAsync(new GameOptionData { InitialDealerSeatNo = 6, Ante = 2, AnteType = AnteType.BigBlind },
            [Stack, Stack, Stack, Stack, Stack, Stack]);

        var hand = (await session.StartAsync()).HandData!;

        hand.Ante.Should().Be(2M);
        hand.Actions.Select(action => (action.No, action.SeatNo, action.ActionType, action.Size, action.Amount)).Should().Equal(
            (1, (byte)1, HandActionType.SmallBlind, 1M, 1M),
            (2, (byte)2, HandActionType.BigBlind, 2M, 2M),
            (3, (byte)2, HandActionType.Ante, 2M, 2M));
        hand.Pot.Should().Be(2M);
        hand.StreetPot.Should().Be(3M);
        hand.NextPlayerSeatNo.Should().Be(3);
        Seat(hand, 2).LastAction!.ActionType.Should().Be(HandActionType.BigBlind, "the ante after the blind is not the seat's last action");
        Seat(hand, 2).BetSize.Should().Be(2M);
        Seat(hand, 2).Stack.Should().Be(Stack - 4);
        Call(hand).Should().Be(2M);
        RaiseWindow(hand).Min.Should().Be(4M);

        foreach (var _ in Enumerable.Range(0, 4))
        {
            await session.PlayAsync(PlayerActionType.Fold);
        }
        hand = (await session.PlayAsync(PlayerActionType.Call)).HandData!;

        hand.Street.Should().Be(Street.PreFlop, "the big blind still has its option");
        hand.NextPlayerSeatNo.Should().Be(2);
        hand.Evaluations.Should().Contain(new PlayerActionEvaluationData(PlayerActionType.Check));
        RaiseWindow(hand).Should().Be((4M, Stack - 2), "a raise to at least 4, and the ante is not part of the stack it can raise to");

        hand = (await session.PlayAsync(PlayerActionType.Check)).HandData!;

        hand.Street.Should().Be(Street.Flop);
        hand.Pot.Should().Be(6M, "the ante plus two big blinds");
        hand.StreetPot.Should().Be(0M);
    }

    [TestMethod]
    public async Task ClassicAnte_ShortStack_AllInOnTheAnteContestsTheMainPotOnly()
    {
        using var session = await AnteSession.CreateAsync(new GameOptionData { InitialDealerSeatNo = 6, Ante = 1, AnteType = AnteType.Classic },
            [0.5M, Stack, Stack, Stack, Stack, Stack]);

        var hand = (await session.StartAsync()).HandData!;

        // Seat 1 would have posted the small blind; all in on its ante, it posts none, and the blinds move on.
        Seat(hand, 1).Stack.Should().Be(0M);
        Seat(hand, 1).Hole.Should().Equal(_holes[1]);
        hand.Actions.Where(action => action.SeatNo == 1).Select(action => (action.ActionType, action.Size, action.Amount))
            .Should().Equal((HandActionType.Ante, 1M, 0.5M));
        hand.SmallBlindSeatNo.Should().Be(2);
        hand.BigBlindSeatNo.Should().Be(3);
        hand.NextPlayerSeatNo.Should().Be(4);
        hand.Pot.Should().Be(5.5M);
        hand.StreetPot.Should().Be(3M);

        // Seats 4, 5 and 6 fold, seat 1 is skipped, the small blind folds: the big blind and seat 1 show down.
        foreach (var _ in Enumerable.Range(0, 4))
        {
            hand = (await session.PlayAsync(PlayerActionType.Fold)).HandData!;
        }

        hand.Street.Should().Be(Street.Finished);
        hand.CommunityCards.Should().Equal(_board);
        hand.Pot.Should().Be(8.5M);

        // Seat 1 wins the pot it contests, 0.5 from each of the six seats dealt; the other 5.5 is the big blind's alone.
        Seat(hand, 1).HandResult!.Net.Should().Be(0.5M * 6 - 0.5M);
        Seat(hand, 3).HandResult!.Net.Should().Be(8.5M - (0.5M * 6) - 3M);
        Seat(hand, 2).HandResult!.Net.Should().Be(-2M);
        new byte[] { 4, 5, 6 }.Select(seatNo => Seat(hand, seatNo).HandResult!.Net).Should().OnlyContain(net => net == -1M);
        hand.Seats.Sum(seat => seat.HandResult!.Net).Should().Be(0M);
    }

    [TestMethod]
    public async Task BigBlindAnte_ShortBigBlind_PostsTheBlindInFullAndTheRestAsAnte()
    {
        using var session = await AnteSession.CreateAsync(new GameOptionData { InitialDealerSeatNo = 6, Ante = 2, AnteType = AnteType.BigBlind },
            [Stack, 3M, Stack, Stack, Stack, Stack]);

        var hand = (await session.StartAsync()).HandData!;

        hand.Actions.Select(action => (action.SeatNo, action.ActionType, action.Size, action.Amount)).Should().Equal(
            ((byte)1, HandActionType.SmallBlind, 1M, 1M),
            ((byte)2, HandActionType.BigBlind, 2M, 2M),
            ((byte)2, HandActionType.Ante, 2M, 1M));
        Seat(hand, 2).Stack.Should().Be(0M, "the big blind is all in");
        Seat(hand, 2).BetSize.Should().Be(2M);
        hand.Pot.Should().Be(1M);
        hand.StreetPot.Should().Be(3M);
        hand.NextPlayerSeatNo.Should().Be(3);
        Call(hand).Should().Be(2M);
        RaiseWindow(hand).Min.Should().Be(4M);
    }

    [TestMethod]
    public async Task BigBlindAnte_AllInAboveTheCaller_TheAnteIsDeadMoneyForTheCaller()
    {
        // The shape of WSOP hand 2023/43/5/03-02-41: the big blind moves all in, its ante on top of the wager lifts
        // its total above the caller's, and the caller has the better hand. Dealer 3, seat 1 the small blind.
        using var session = await AnteSession.CreateAsync(new GameOptionData { InitialDealerSeatNo = 3, Ante = 3, AnteType = AnteType.BigBlind },
            [Stack, 25M, Stack]);

        await session.StartAsync();
        await session.PlayAsync(PlayerActionType.Fold);
        await session.PlayAsync(PlayerActionType.Raise, 10);
        await session.PlayAsync(PlayerActionType.AllIn);
        var hand = (await session.PlayAsync(PlayerActionType.Call)).HandData!;

        hand.Street.Should().Be(Street.Finished);
        hand.CommunityCards.Should().Equal(_board);
        Seat(hand, 2).HandResult!.Net.Should().Be(-25M, "the big blind's ante is not its own to take back");
        Seat(hand, 1).HandResult!.Net.Should().Be(25M, "the aces win the 22 wagered by each seat and the ante of 3");
        Seat(hand, 3).HandResult!.Net.Should().Be(0M);
    }

    [TestMethod]
    public async Task ClassicAnte_HeadsUp_BothSeatsPostBeforeTheBlinds()
    {
        using var session = await AnteSession.CreateAsync(new GameOptionData { MaxPlayersCount = 2, InitialDealerSeatNo = 1, Ante = 1, AnteType = AnteType.Classic },
            [Stack, Stack]);

        var hand = (await session.StartAsync()).HandData!;

        hand.Actions.Select(action => (action.SeatNo, action.ActionType, action.Amount)).Should().Equal(
            ((byte)1, HandActionType.Ante, 1M),
            ((byte)2, HandActionType.Ante, 1M),
            ((byte)1, HandActionType.SmallBlind, 1M),
            ((byte)2, HandActionType.BigBlind, 2M));
        hand.DealerSeatNo.Should().Be(1);
        hand.SmallBlindSeatNo.Should().Be(1, "heads-up the button posts the small blind");
        hand.NextPlayerSeatNo.Should().Be(1, "and acts first pre-flop");
        hand.Pot.Should().Be(2M);
        hand.StreetPot.Should().Be(3M);
        Call(hand).Should().Be(1M);
        RaiseWindow(hand).Min.Should().Be(4M);

        await session.PlayAsync(PlayerActionType.Call);
        hand = (await session.PlayAsync(PlayerActionType.Check)).HandData!;

        hand.Street.Should().Be(Street.Flop);
        hand.NextPlayerSeatNo.Should().Be(2, "the big blind acts first after the flop");
        hand.Pot.Should().Be(6M);
    }

    [TestMethod]
    public async Task BigBlindAnte_HeadsUp_TheBigBlindSeatPostsIt()
    {
        using var session = await AnteSession.CreateAsync(new GameOptionData { MaxPlayersCount = 2, InitialDealerSeatNo = 1, Ante = 2, AnteType = AnteType.BigBlind },
            [Stack, Stack]);

        var hand = (await session.StartAsync()).HandData!;

        hand.Actions.Select(action => (action.SeatNo, action.ActionType, action.Amount)).Should().Equal(
            ((byte)1, HandActionType.SmallBlind, 1M),
            ((byte)2, HandActionType.BigBlind, 2M),
            ((byte)2, HandActionType.Ante, 2M));
        hand.SmallBlindSeatNo.Should().Be(1);
        hand.BigBlindSeatNo.Should().Be(2);
        hand.NextPlayerSeatNo.Should().Be(1);
        hand.Pot.Should().Be(2M);
        hand.StreetPot.Should().Be(3M);
        Call(hand).Should().Be(1M);

        hand = (await session.PlayAsync(PlayerActionType.Call)).HandData!;
        hand.NextPlayerSeatNo.Should().Be(2, "the big blind keeps its option");
        hand.Evaluations.Should().Contain(new PlayerActionEvaluationData(PlayerActionType.Check));

        hand = (await session.PlayAsync(PlayerActionType.Check)).HandData!;
        hand.Street.Should().Be(Street.Flop);
        hand.NextPlayerSeatNo.Should().Be(2);
        hand.Pot.Should().Be(6M);
    }

    [TestMethod]
    public async Task ClassicAnte_HeadsUp_SeatAllInOnTheAnte_HandRunsOut()
    {
        using var session = await AnteSession.CreateAsync(new GameOptionData { MaxPlayersCount = 2, InitialDealerSeatNo = 1, Ante = 1, AnteType = AnteType.Classic },
            [0.5M, Stack]);

        // With one seat left that can post, the blind seats are chosen first and seat 1 posts a blind of nothing.
        var hand = (await session.StartAsync()).HandData!;

        hand.Street.Should().Be(Street.Finished, "nobody is left to bet against");
        hand.CommunityCards.Should().Equal(_board);
        Seat(hand, 1).HandResult!.Net.Should().Be(0.5M, "seat 1 wins the 1 both seats put in up to its 0.5");
        Seat(hand, 2).HandResult!.Net.Should().Be(-0.5M, "the rest of seat 2's ante and its big blind come back to it");
    }

    [TestMethod]
    public async Task ClassicAnte_PotLimit_TheAntesRaiseThePotLimitMaximum()
    {
        using var session = await AnteSession.CreateAsync(new GameOptionData
        {
            BettingStructure = BettingStructure.PotLimit,
            InitialDealerSeatNo = 6,
            Ante = 1,
            AnteType = AnteType.Classic,
        }, [Stack, Stack, Stack, Stack, Stack, Stack]);

        var hand = (await session.StartAsync()).HandData!;

        // The standing wager, 2, plus the pot once seat 3 has called: the six antes, the blinds' 3 and the call's 2.
        const decimal standing = 2M;
        const decimal potAfterCall = (6 * 1M) + 1M + 2M + standing;
        RaiseWindow(hand).Should().Be((4M, standing + potAfterCall));
        RaiseWindow(hand).Max.Should().Be(13M, "without the antes it would be 7");
    }

    [TestMethod]
    public async Task ClassicAnte_BlindSchedule_TheAnteKeepsItsRatioToTheSmallBlind()
    {
        using var session = await AnteSession.CreateAsync(new GameOptionData
        {
            MaxPlayersCount = 3,
            InitialDealerSeatNo = 3,
            Ante = 1,
            AnteType = AnteType.Classic,
            BlindSchedule = new BlindScheduleData { IncreaseEveryHands = 1, Mode = BlindIncreaseMode.Multiplier, Multiplier = 2 },
        }, [Stack, Stack, Stack]);

        var hand = (await session.StartAsync()).HandData!;
        hand.SmallBlind.Should().Be(1M);
        hand.Ante.Should().Be(1M);

        await session.PlayAsync(PlayerActionType.Fold);
        (await session.PlayAsync(PlayerActionType.Fold)).HandData!.Street.Should().Be(Street.Finished);

        hand = (await session.StartAsync()).HandData!;

        hand.No.Should().Be(2);
        hand.SmallBlind.Should().Be(2M);
        hand.Ante.Should().Be(2M);
        hand.Actions.Where(action => action.ActionType == HandActionType.Ante).Should().HaveCount(3)
            .And.OnlyContain(action => action.Size == 2M && action.Amount == 2M);
        hand.Pot.Should().Be(6M);
    }

    [TestMethod]
    public async Task ClassicAnte_Rehydration_EveryCallMatchesAFreshLoadOfTheLedger()
    {
        using var session = await AnteSession.CreateAsync(new GameOptionData
        {
            BettingStructure = BettingStructure.PotLimit,
            InitialDealerSeatNo = 6,
            Ante = 1,
            AnteType = AnteType.Classic,
        }, [0.5M, Stack, Stack, Stack, Stack, Stack]);

        // Dealer 6 and seat 1 all in on its ante: seat 2 posts the small blind, seat 3 the big blind, seat 4 opens.
        var steps = new List<Func<Task<GameData>>>
        {
            session.StartAsync,
            () => session.PlayAsync(PlayerActionType.Raise, 5),
            () => session.PlayAsync(PlayerActionType.Call),
            () => session.PlayAsync(PlayerActionType.Fold),
            () => session.PlayAsync(PlayerActionType.Fold),
            () => session.PlayAsync(PlayerActionType.Call),
            () => session.PlayAsync(PlayerActionType.Check),
            () => session.PlayAsync(PlayerActionType.Bet, 10),
            () => session.PlayAsync(PlayerActionType.Call),
        };

        GameData? last = null;
        foreach (var step in steps)
        {
            last = await step();
            var reloaded = await session.GetAsync();

            // The returned data comes from the graph the call just mutated, the reload from the ledger alone.
            reloaded.ShouldAsJToken().BeEquivalentToAsJToken(last);
        }

        var hand = last!.HandData!;
        hand.Street.Should().Be(Street.Flop);
        hand.NextPlayerSeatNo.Should().Be(3);
        hand.Pot.Should().Be(5.5M + 1M + (3 * 5M), "the antes, the folded small blind and three pre-flop wagers of 5");
        hand.StreetPot.Should().Be(2 * 10M);

        // The pot-limit maximum reads Committed, which the reload rebuilt from the ante rows as well: the bet of 10
        // plus the pot after seat 3 calls it, 21.5 + 20 + 10.
        RaiseWindow(hand).Should().Be((20M, 10M + 21.5M + 20M + 10M));
    }

    private static SeatData Seat(HandData hand, byte seatNo) => hand.Seats.Single(seat => seat.SeatNo == seatNo);

    private static decimal Call(HandData hand)
    {
        var call = hand.Evaluations.Single(evaluation => evaluation.PlayerActionType == PlayerActionType.Call);
        call.MinValue.Should().Be(call.MaxValue);
        return call.MinValue;
    }

    private static (decimal Min, decimal Max) RaiseWindow(HandData hand)
    {
        var raise = hand.Evaluations.Single(evaluation => evaluation.PlayerActionType == PlayerActionType.Raise);
        return (raise.MinValue, raise.MaxValue);
    }

    private static Card[] CardsFrom(params string[] cards) => [.. cards.Select(Card.Parse)];

    /// <summary>One game over its own test host, dealing <see cref="_holes"/> and <see cref="_board"/> every hand.</summary>
    private sealed class AnteSession : IDisposable
    {
        private static readonly Guid _gameId = Guid.Parse("{a17ea17e-a17e-a17e-a17e-a17ea17ea17e}");

        private readonly TestEngineHost.DisposableTestData _host;
        private readonly IHoldemTable _engine;
        private readonly PlayerJoinData[] _joinSeats;
        private bool _started;

        private AnteSession(decimal[] stacks)
        {
            _joinSeats = [.. stacks.Select((stack, index) => new PlayerJoinData($"P{index + 1}", (byte)(index + 1), stack))];
            _host = TestEngineHost.Setup(out _engine, out _, shuffler: new TestCardShuffler(
                getHoleFunc: (_, seatNo) => _holes[seatNo],
                getDeckFunc: _ => _board));
        }

        internal static async Task<AnteSession> CreateAsync(GameOptionData options, decimal[] stacks)
        {
            var session = new AnteSession(stacks);
            await session.ApplyAsync(new HoldemActionCreateGame(_gameId, options with
            {
                MaxPlayersCount = (byte)stacks.Length,
                InitialSmallBlind = 1,
            }));
            return session;
        }

        internal Task<GameData> StartAsync()
        {
            HoldemAction action = _started ? new HoldemActionStartHand(_gameId) : new HoldemActionStartGame(_gameId, _joinSeats);
            _started = true;
            return ApplyAsync(action);
        }

        internal Task<GameData> PlayAsync(PlayerActionType actionType, decimal size = 0)
            => ApplyAsync(new HoldemActionPlay(_gameId, actionType, size));

        internal Task<GameData> GetAsync() => ApplyAsync(new HoldemActionGetGame(_gameId));

        private async Task<GameData> ApplyAsync(HoldemAction holdemAction)
        {
            var result = await _engine.ApplyAsync(holdemAction);

            result.IsSuccess.Should().BeTrue(
                $"{holdemAction.GetType().Name} should be accepted: " +
                string.Join("; ", result.Errors.Select(error => error.Message)));

            return result.Value;
        }

        public void Dispose() => _host.Dispose();
    }
}
