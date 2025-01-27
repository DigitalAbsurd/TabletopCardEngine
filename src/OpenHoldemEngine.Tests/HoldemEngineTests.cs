using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;
using OpenHoldemEngine.Options;
using OpenHoldemEngine.Primitives;

namespace OpenHoldemEngine.Tests;

[TestClass]
public sealed class HoldemEngineTests
{
    [TestMethod]
    public async Task ApplyHoldemActionAsync_StartGame()
    {
        var gameId = Guid.Parse("{11111111-1111-1111-1111-111111111111}");
        var gameOptions = new GameOptionData(
                MaxPlayersCount: 10,
                InitialSmallBlind: 5,
                InitialDealerSeatNo: 3,
                AllowRebuy: true,
                MaxHandsCount: 1000,
                AutoIncrementsHands: 5);

        var seatsData = new PlayerJoinData[]
        {
            new PlayerJoinData("Junpei", 1, 100),
            new PlayerJoinData("Geraldine", 2, 100),
            new PlayerJoinData("Zuhaitz", 3, 100),
            new PlayerJoinData("Ozias", 4, 100),
            new PlayerJoinData("Ilona", 5, 100),
            new PlayerJoinData("Broen", 6, 100)
        };

        var holeCards = new Dictionary<byte, Card[]>()
        {
            { 1, CardsFromStrings([ "7h", "td" ]) },
            { 2, CardsFromStrings([ "8s", "2s" ]) },
            { 3, CardsFromStrings([ "4h", "9d" ]) },
            { 4, CardsFromStrings([ "jh", "kd" ]) },
            { 5, CardsFromStrings([ "7s", "5d" ]) },
            { 6, CardsFromStrings([ "3h", "ts" ]) },
        };

        var expected = new GameData(
            GameId: gameId,
            Options: gameOptions,
            HandData: new HandData(
                No: 1,
                Seats: new SeatData[]
                {
                    new SeatData(1, "Junpei",  100, 100)
                    {
                        Hole = holeCards[1]
                    },
                    new SeatData(2, "Geraldine", 100, 100)
                    {
                        Hole = holeCards[2]
                    },
                    new SeatData(3, "Zuhaitz", 100, 100)
                    {
                        Hole = holeCards[3]
                    },
                    new SeatData(4, "Ozias", 100, 95)
                    {
                        Hole = holeCards[4]
                    },
                    new SeatData(5, "Ilona", 100, 90)
                    {
                        Hole = holeCards[5]
                    },
                    new SeatData(6, "Broen", 100, 100)
                    {
                        Hole = holeCards[6]
                    },
                },
                DealerSeatNo: 3,
                SmallBlindSeatNo: 4,
                BigBlindSeatNo: 5,
                NextPlayerSeatNo: 6,
                Street: Primitives.Street.PreFlop,
                StreetPot: 15,
                Pot: 15,
                CommunityCards: Array.Empty<Card>(),
                Evaluations: new PlayerActionEvaluationData[]
                {
                    new PlayerActionEvaluationData(PlayerActionType.Fold),
                    new PlayerActionEvaluationData(PlayerActionType.Call, 10),
                    new PlayerActionEvaluationData(PlayerActionType.Raise, 15, 100)
                })
            );

        using var holdemServices = SetupHoldemEngineServices(out var holdemEngine, holeCards);

        await holdemEngine.ApplyHoldemActionAsync(
            new HoldemActionCreateGame(
                GameId: gameId,
                Options: gameOptions));

        var actual = await holdemEngine.ApplyHoldemActionAsync(
            new HoldemActionStartHand(GameId: gameId)
            {
                JoinSeats = seatsData
            });

        expected.ShouldAsJToken().BeEquivalentToAsJToken(actual);
    }

    [TestMethod, DoNotParallelize]
    [DynamicData(nameof(GetPapEmulatedHandData), DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(GetPapTestDisplayName))]
    public async Task TestWithEmulatedPAPGame(Helpers.PapGameData papGame, int handNo)
    {
        var gameId = Guid.Parse("{11111111-1111-1111-1111-111111111111}");
        var papHand = papGame.Hands.Single(h => h.HandNo == handNo);
        Action<HoldemDbContext> seedAction = ctx =>
        {
            ctx.Add(new Game
            {
                Id = gameId,
                AllowRebuy = true,
                AutoIncrementHands = 0,
                InitialSmallBlind = papGame.InitialSmallBlind,
                MaxHandsCount = 0,
                MaxPlayersCount = papGame.PlayersMap.Count,
                InitialDealerSeatNo = papGame.StartDealerSeatNo,
                Players = papGame.PlayersMap.Select(pm => new Player { Name = pm.Key, SeatNo = pm.Value, RoundNoStart = 0 }).ToList()
            });

            if (handNo > 1)
                ctx.Set<HandPlayerEndState>()
                    .AddRange(papGame.PlayersMap.Values.Select(seatNo =>
                    new HandPlayerEndState
                    {
                        GameId = gameId,
                        HandNo = handNo - 1,
                        SeatNo = seatNo
                    }));

            ctx.SaveChanges();
        };

        using var holdemServices = SetupHoldemEngineServices(out var holdemEngine, papHand.HoleCards, seedAction: seedAction);

        using (new AssertionScope())
        {
            var actionResult = await holdemEngine.ApplyHoldemActionAsync(new HoldemActionStartHand(gameId)
            {
                BuyIns = papHand.StartStacks.Select(buyIn => new BuyInData(buyIn.Key, buyIn.Value)).ToArray()
            });

            papHand.ButtonSeatNo.Should().Be(actionResult.HandData!.DealerSeatNo,
                because: $"Button expected {papHand.ButtonSeatNo}, actual {actionResult.HandData!.DealerSeatNo}");

            papHand.SmallBlindSeatNo.Should().Be(actionResult.HandData!.SmallBlindSeatNo,
                because: $"SB expected {papHand.SmallBlindSeatNo}, actual {actionResult.HandData!.SmallBlindSeatNo}");

            papHand.BigBlindSeatNo.Should().Be(actionResult.HandData!.BigBlindSeatNo,
                because: $"BB expected {papHand.BigBlindSeatNo}, actual {actionResult.HandData!.BigBlindSeatNo}");
        }



        //foreach (var emulatedAction in papHand.ActionStack)
        //{

        //}
    }

    private IDisposable SetupHoldemEngineServices(out HoldemEngine holdemEngine,
        Dictionary<byte, Card[]>? holeCards = null,
        Dictionary<Street, Card[]>? communityCards = null,
        Action<HoldemEngineSettings>? configureSettings = null,
        Action<HoldemDbContext>? seedAction = null)
    {
        var db3FileName = Environment.GetEnvironmentVariable("OHE_TEST_SQLITE_DB3_FILE_NAME");
        var keepAliveConnection = string.IsNullOrEmpty(db3FileName) ?
            new SqliteConnection("DataSource=:memory:") :
            default(SqliteConnection);

        IServiceCollection services = new ServiceCollection();
        services.AddHoldemEngine(
            dbOptionsAction: opts =>
            {
#if DEBUG
                opts.LogTo(msg => System.Diagnostics.Debug.WriteLine(msg), minimumLevel: Microsoft.Extensions.Logging.LogLevel.Information);
                opts.EnableSensitiveDataLogging();
#endif
                if (keepAliveConnection != default)
                {
                    opts.UseSqlite(keepAliveConnection);
                }
                else
                {
                    opts.UseSqlite($"Filename='{db3FileName}'");
                }
                //if (seedAction != null)
                //    opts.UseSeeding((ctx, _) => seedAction((HoldemDbContext)ctx));
            },
            configure: opts =>
            {
                if (configureSettings != null)
                    opts.WithSettings(configureSettings);
                if (holeCards != null)
                    opts.WithCardShuffler(new TestCardShuffler(holeCards, communityCards));
            }
            );
        var serviceProvider = services.BuildServiceProvider();
        using var dbContext = serviceProvider.GetRequiredService<IDbContextFactory<HoldemDbContext>>().CreateDbContext();
        keepAliveConnection?.Open();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        if (seedAction != null)
            seedAction(dbContext);

        holdemEngine = serviceProvider.GetRequiredService<HoldemEngine>();

        return new DisposableTestData(serviceProvider, keepAliveConnection);
    }

    private static IEnumerable<object[]> GetPapEmulatedHandData()
    {
        var papEmulatedGame = Helpers.GetPAPEmulatedGameData();
        var papHandEnumerator = papEmulatedGame.Hands.GetEnumerator();
        while (papHandEnumerator.MoveNext())
        {
            yield return new object[] { papEmulatedGame, papHandEnumerator.Current.HandNo };
            yield break;
        }
    }

    public static string GetPapTestDisplayName(System.Reflection.MethodInfo methodInfo, Object[] values)
        => $"{methodInfo.Name}_{values[1]:D3}";

    private class DisposableTestData(ServiceProvider _serviceProvider, SqliteConnection? _sqliteConnection) : IDisposable
    {
        public void Dispose()
        {
            _sqliteConnection?.Dispose();
            _serviceProvider?.Dispose();
        }
    }

    private static Card[] CardsFromStrings(params string[] cardStrings)
    {
        var cardToStringConverter = new Converters.CardToStringConverter();

        if (cardStrings == null)
            return Array.Empty<Card>();

        return cardStrings.Select(str => (Card)cardToStringConverter.ConvertFromProvider(new string(str.Reverse().ToArray()))!).ToArray()!;
    }

    class TestCardShuffler(Dictionary<byte, Card[]> _holeCards, Dictionary<Street, Card[]>? _communityCards = null) : ICardShuffler
    {
        public Task<IEnumerable<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo)
        {
            return Task<IEnumerable<Card>>.FromResult(_holeCards[seatNo].AsEnumerable());
        }

        public Task<IEnumerable<Card>?> GetStreetCardsAsync(Guid gameId, int handNo, Street street)
        {
            return Task<IEnumerable<Card>?>.FromResult(_communityCards?[street]?.AsEnumerable());
        }
    }
}
