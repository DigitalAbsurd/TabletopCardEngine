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
                AllowRebuy: true,
                MaxHandsCount: 1000,
                AutoIncrementsHands: 5);

        var playersData = new PlayerJoinData[]
        {
            new PlayerJoinData("Junpei", 1),
            new PlayerJoinData("Geraldine", 2),
            new PlayerJoinData("Zuhaitz", 3),
            new PlayerJoinData("Ozias", 4),
            new PlayerJoinData("Ilona", 5),
            new PlayerJoinData("Broen", 6)
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
                Seats: new SeatData[]
                {
                    new SeatData(1, "Junpei",  100, 100)
                    { 
                        Hole = holeCards[1]
                    },
                    new SeatData(2, "Geraldine", 100, 95)
                    {
                        Hole = holeCards[2]
                    },
                    new SeatData(3, "Zuhaitz", 100, 90)
                    { 
                        Hole = holeCards[3] 
                    },
                    new SeatData(4, "Ozias", 100, 100) 
                    { 
                        Hole = holeCards[4] 
                    },
                    new SeatData(5, "Ilona", 100, 100) 
                    { 
                        Hole = holeCards[5] 
                    },
                    new SeatData(6, "Broen", 100, 100) 
                    { 
                        Hole = holeCards[6] 
                    },
                },
                DealerSeatNo: 1,
                SmallBlindSeatNo: 2,
                BigBlindSeatNo: 3,
                NextPlayerSeatNo: 4,
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

        var actual = await holdemEngine.ApplyHoldemActionAsync(
            new HoldemActionStartGame(
                GameId: gameId,
                Seats: playersData,
                BuyIns: playersData.Select(pd => new BuyInData(pd.SeatNo, 100)).ToArray(),
                Options: gameOptions));

        expected.ShouldAsJToken().BeEquivalentToAsJToken(actual);
    }

    [TestMethod]
    public async Task TestWithEmulatedPAPGame()
    {
        var emulatedGame = Helpers.GetPAPEmulatedGameData();
    }

    private IDisposable SetupHoldemEngineServices(out HoldemEngine holdemEngine, 
        Dictionary<byte, Card[]>? holeCards = null,
        Dictionary<Street, Card[]>? communityCards = null,
        Action<HoldemEngineSettings> configureSettings = null!)
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

        holdemEngine = serviceProvider.GetRequiredService<HoldemEngine>();

        return new DisposableTestData(serviceProvider, keepAliveConnection);
    }

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
