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

        var deckCards = CardsFromStrings(["ac", "ad", "as", "ah", "2d"]);

        var expectedGameData = new GameData(
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
                        Hole = holeCards[4],
                        LastAction = new PlayerActionData(Street.PreFlop, PlayerActionType.SmallBlind, 5, 1)
                    },
                    new SeatData(5, "Ilona", 100, 90)
                    {
                        Hole = holeCards[5],
                        LastAction = new PlayerActionData(Street.PreFlop, PlayerActionType.BigBlind, 10, 2)
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
                    new PlayerActionEvaluationData(PlayerActionType.Call, 10, 10),
                    new PlayerActionEvaluationData(PlayerActionType.Raise, 15, 100)
                })
            );


        GameData actualGameData;
        using (SetupHoldemEngineServices(out var holdemEngine, out _,
                shuffler: new TestCardShuffler(_getHoleFunc: (no, seatNo) => holeCards[seatNo], _getDeckFunc: handNo => deckCards)))
        {
            await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionCreateGame(
                    GameId: gameId,
                    Options: gameOptions));

            actualGameData = (await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionStartHand(GameId: gameId)
                {
                    JoinSeats = seatsData
                })).Value;
        }

        expectedGameData.ShouldAsJToken().BeEquivalentToAsJToken(actualGameData);
    }

    [TestMethod]
    public async Task EmulatedPAPGame_Test()
    {
        var gameId = Guid.Parse("{11111111-1111-1111-1111-111111111111}");
        var emulatedGame = Helpers.GetPAPEmulatedGameData();
        var seats = emulatedGame.PlayersMap
            .Select(pm => new PlayerJoinData(pm.Key, pm.Value, emulatedGame.Hands[0].StartStacks[pm.Value]))
            .ToArray();
        GameData gameData;
        (Dictionary<byte, Card[]> holeCards, Card[] deckCards) currentDeck = new();
        var shuffler = new TestCardShuffler(
            _getHoleFunc: (handNo, seatNo) => currentDeck.holeCards![seatNo],
            _getDeckFunc: (handNo) => currentDeck.deckCards!);

        using (SetupHoldemEngineServices(out var holdemEngine, out var dbContextFactory, shuffler: shuffler))
        {
            await holdemEngine.ApplyHoldemActionAsync(
                new HoldemActionCreateGame(
                    GameId: gameId,
                    Options: new GameOptionData(
                        MaxPlayersCount: emulatedGame.PlayersMap.Count,
                        InitialSmallBlind: emulatedGame.InitialSmallBlind,
                        InitialDealerSeatNo: emulatedGame.StartDealerSeatNo,
                        AllowRebuy: false)));

            foreach (var emulatedHand in emulatedGame.Hands)
            {
                currentDeck = GenerateHandCards(emulatedHand);
                using (new AssertionScope($"PAP Hand #{emulatedHand.HandNo}"))
                {
                    gameData = (await holdemEngine.ApplyHoldemActionAsync(new HoldemActionStartHand(gameId)
                    {
                        JoinSeats = emulatedHand.HandNo == 1 ? seats : []
                    })).Value;

                    //PAP emulated game has a bug with dealer button #94, #111
                    if (CorrectButtonIfNeeded(dbContextFactory))
                        gameData = (await holdemEngine.ApplyHoldemActionAsync(new HoldemActionGetGame(gameId))).Value;

                    foreach (var startStackItem in emulatedHand.StartStacks)
                    {
                        startStackItem.Value.Should()
                            .Be(gameData.HandData.Seats.Single(seat => seat.SeatNo == startStackItem.Key).StartRoundStack,
                            because: $"{emulatedGame.GetNameBySeatNo(startStackItem.Key)} SeatNo:{startStackItem.Key} StartStack assert");
                    }

                    emulatedHand.ButtonSeatNo.Should().Be(gameData.HandData.DealerSeatNo,
                        because: $"Button expected {emulatedHand.ButtonSeatNo}, actual {gameData.HandData.DealerSeatNo}");

                    emulatedHand.SmallBlindSeatNo.Should().Be(gameData.HandData.SmallBlindSeatNo,
                        because: $"SB expected {emulatedHand.SmallBlindSeatNo}, actual {gameData.HandData.SmallBlindSeatNo}");

                    emulatedHand.BigBlindSeatNo.Should().Be(gameData.HandData.BigBlindSeatNo,
                        because: $"BB expected {emulatedHand.BigBlindSeatNo}, actual {gameData.HandData.BigBlindSeatNo}");

                    foreach (var emulatedAction in emulatedHand.ActionStack)
                    {
                        var scopeContext = $"ActNo#{(emulatedAction.No + 2)}";

                        //emulatedAction.SeatNo = 0 means that parsed PAP action section is fold groups (ex. 2 folds)
                        if (emulatedAction.SeatNo != 0)
                        {
                            emulatedAction.SeatNo.Should().Be(gameData.HandData.NextPlayerSeatNo,
                                because: $"{scopeContext} SeatNo expected {emulatedAction.SeatNo}, actual {gameData.HandData.NextPlayerSeatNo}");
                        }

                        gameData = (await holdemEngine.ApplyHoldemActionAsync(
                                new HoldemActionPlayerAction(gameId, emulatedAction.ActionType, emulatedAction.Size))).Value;

                        var handLastAction = gameData.HandData.GetLastAction();
                        emulatedAction.Street.Should().Be(handLastAction.Street,
                            because: $"{scopeContext} Street expected {emulatedAction.SeatNo}, actual {handLastAction.Street}");
                    }
                }
            }
        }

        bool CorrectButtonIfNeeded(IDbContextFactory<HoldemDbContext> dbContextFactory)
        {
            if (gameData.HandData.No == 94)
            {
                var dbContext = dbContextFactory.CreateDbContext();

                dbContext.Database.ExecuteSqlRaw($"UPDATE Hand SET DealerSeatNo=0 WHERE GameId='{gameId}' AND No=94");
                dbContext.Database.ExecuteSqlRaw($"UPDATE HandAction SET SeatNo=3, NextSeatNo=4 WHERE GameId='{gameId}' AND HandNo=94 AND PlayAction='SmallBlind'");
                dbContext.Database.ExecuteSqlRaw($"UPDATE HandAction SET SeatNo=4, NextSeatNo=6 WHERE GameId='{gameId}' AND HandNo=94 AND PlayAction='BigBlind'");
                return true;
            }

            if (gameData.HandData.No == 111)
            {
                var dbContext = dbContextFactory.CreateDbContext();

                dbContext.Database.ExecuteSqlRaw($"UPDATE Hand SET DealerSeatNo=6 WHERE GameId='{gameId}' AND No=111");
                dbContext.Database.ExecuteSqlRaw($"UPDATE HandAction SET SeatNo=6, NextSeatNo=4 WHERE GameId='{gameId}' AND HandNo=111 AND PlayAction='SmallBlind'");
                dbContext.Database.ExecuteSqlRaw($"UPDATE HandAction SET SeatNo=4, NextSeatNo=6 WHERE GameId='{gameId}' AND HandNo=111 AND PlayAction='BigBlind'");
                return true;
            }

            return false;
        }

        (Dictionary<byte, Card[]> holeCards, Card[] deckCards) GenerateHandCards(Helpers.PapHandData emulatedHand)
        {
            var rnd = new Random();
            var deck = Enum.GetValues<CardType>()
                .SelectMany(cType => Enum.GetValues<CardSuit>().Select(cSuit => new Card { CardSuit = cSuit, CardType = cType }))
                .ToList();
            var seatHoles = emulatedHand.HoleCards.ToDictionary(k => k.Key, v => v.Value);
            var deckCards = emulatedHand.DeckCards.Select(v => v.Value).OrderBy(dc => dc.Length).LastOrDefault() ?? [];

            deck = deck.Except(seatHoles.SelectMany(sh => sh.Value)).Except(deckCards).ToList();

            foreach (var seat in emulatedGame.PlayersMap.Where(pm => !seatHoles.ContainsKey(pm.Value)).Select(pm => pm.Value))
            {
                seatHoles[seat] = GetRandomCardsFromDeck(2);
            }

            deckCards = deckCards.Concat(GetRandomCardsFromDeck(5 - deckCards.Length)).ToArray();

            return (seatHoles, deckCards);

            Card[] GetRandomCardsFromDeck(int num)
            {
                var rndCards = new List<Card>();
                for (int ix = 0; ix < num; ix++)
                {
                    var rndCard = deck[rnd.Next(deck.Count)];
                    deck = deck.Except([rndCard]).ToList();
                    rndCards.Add(rndCard);
                }
                return rndCards.ToArray();
            }
        }
    }

    private IDisposable SetupHoldemEngineServices(out HoldemEngine holdemEngine,
        out IDbContextFactory<HoldemDbContext> contextFactory,
        ICardShuffler? shuffler = null,
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
                if (bool.TryParse(Environment.GetEnvironmentVariable("OHE_TEST_SQL_DEBUG"), out var dbgSql) && dbgSql)
                {
                    opts.LogTo(msg => System.Diagnostics.Debug.WriteLine(msg), minimumLevel: Microsoft.Extensions.Logging.LogLevel.Information);
                    opts.EnableSensitiveDataLogging();
                }
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
                if (shuffler != null)
                    opts.WithCardShuffler(shuffler);
            }
            );
        var serviceProvider = services.BuildServiceProvider();
        contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();
        using var dbContext = contextFactory.CreateDbContext();
        keepAliveConnection?.Open();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        if (seedAction != null)
            seedAction(dbContext);

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

    class TestCardShuffler(Func<int, byte, Card[]> _getHoleFunc, Func<int, Card[]> _getDeckFunc) : ICardShuffler
    {
        public Task<IEnumerable<Card>> GetHoleCardsAsync(Guid gameId, int handNo, byte seatNo)
        {
            return Task<IEnumerable<Card>>.FromResult(_getHoleFunc(handNo, seatNo).AsEnumerable());
        }

        public Task<IEnumerable<Card>> GetDeckCardsAsync(Guid gameId, int handNo)
        {
            return Task<IEnumerable<Card>>.FromResult(_getDeckFunc.Invoke(handNo).AsEnumerable());
        }
    }
}
