using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Demo.Engine;
using TabletopCardEngine.Demo.Phh;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.Demo.Tests;

[TestClass]
public sealed class PhhAnteRoundTrip_Tests
{
    // Hand 1 of wsop-nolimit.phhs.gz (2023 WSOP Event #43, Day 5): five-handed, a big blind ante of 120000, a showdown.
    private const string WsopHand = """
[1]
variant = 'NT'
ante_trimming_status = false
antes = [0, 120000, 0, 0, 0]
blinds_or_straddles = [40000, 80000, 0, 0, 0]
min_bet = 80000
starting_stacks = [7380000, 2500000, 5110000, 10170000, 4545000]
actions = ['d dh p1 7s4s', 'd dh p2 Js8h', 'd dh p3 Td8c', 'd dh p4 6d5h', 'd dh p5 Qh7h', 'p3 f', 'p4 cbr 170000', 'p5 f', 'p1 f', 'p2 cc', 'd db JcTs2d', 'p2 cc', 'p4 cbr 140000', 'p2 cc', 'd db As', 'p2 cc', 'p4 cbr 325000', 'p2 cc', 'd db Qs', 'p2 cc', 'p4 cbr 600000', 'p2 cc', 'p4 sm 6d5h', 'p2 sm Js8h']
author = 'Juho Kim'
event = '2023 World Series of Poker Event #43: $50,000 Poker Players Championship | Day 5'
city = 'Las Vegas'
region = 'Nevada'
country = 'United States of America'
day = 22
month = 6
year = 2023
hand = 1
players = ['Matthew Ashton', 'Kristopher Tong', 'James Obst', 'Talal Shakerchi', 'Brian Rast']
finishing_stacks = [7340000, 3775000, 5110000, 8935000, 4545000]
""";

    private static (DemoGameSession Session, PhhCardShuffler Shuffler) CreateSession()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(
            dbOptionsAction: opts => opts.UseHoldemSqlite(connection),
            configure: _ => { }));
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();

        var shuffler = new PhhCardShuffler();
        var replayEngine = DemoReplayEngine.Create(connection, shuffler);
        return (new DemoGameSession(provider.GetRequiredService<IHoldemTable>(), replayEngine), shuffler);
    }

    /// <summary>Plays one hand to the end: the first seat able to raise does, then everybody calls or checks down.</summary>
    private static async Task<DemoGameSession> PlayHandAsync(int players, AnteType anteType)
    {
        var (session, _) = CreateSession();
        var options = new GameOptionData
        {
            BettingStructure = BettingStructure.NoLimit,
            TableFormat = players == 2 ? HoldemTableFormat.HeadsUp : HoldemTableFormat.SixMax,
            MaxPlayersCount = (byte)players,
            InitialSmallBlind = 50m,
            InitialDealerSeatNo = (byte)players,
            Ante = 25m,
            AnteType = anteType,
        };

        await session.CreateGameAsync(options, Guid.NewGuid());
        await session.StartFirstHandAsync(
        [.. Enumerable.Range(1, players).Select(seatNo => new PlayerJoinData(SeatNo: (byte)seatNo, Name: $"Player {seatNo}", BuyInAmount: 5000m))]);

        var raised = false;
        while (session.CurrentHand!.NextPlayerSeatNo != 0)
        {
            var evaluations = session.CurrentHand.Evaluations;
            var raise = evaluations.FirstOrDefault(e => e.PlayerActionType == PlayerActionType.Raise);
            if (!raised && raise != null)
            {
                raised = true;
                (await session.ApplyPlayerActionAsync(PlayerActionType.Raise, raise.MinValue)).Should().BeTrue();
                continue;
            }

            var type = evaluations.Any(e => e.PlayerActionType == PlayerActionType.Check) ? PlayerActionType.Check : PlayerActionType.Call;
            (await session.ApplyPlayerActionAsync(type)).Should().BeTrue();
        }

        return session;
    }

    private static Dictionary<string, decimal> FinalStacks(GameData game)
        => game.HandData!.Seats.ToDictionary(s => s.Name, s => s.StartHandStack + s.HandResult!.Net);

    [TestMethod]
    [DataRow(6, AnteType.Classic)]
    [DataRow(2, AnteType.BigBlind)]
    public async Task WriteSession_WithAntes_RoundTripsThroughTheReaderAndReplaysToTheSameStacks(int players, AnteType anteType)
    {
        // Arrange
        var played = await PlayHandAsync(players, anteType);
        var expectedStacks = FinalStacks(played.CurrentGame!);
        var posted = played.CurrentHand!.Actions.Count(action => action.ActionType == HandActionType.Ante);
        posted.Should().Be(anteType == AnteType.Classic ? players : 1);

        // Act
        var text = PhhWriter.WriteSession(played.CurrentGame!, played.Journal);
        var hand = PhhReader.ReadAll(text).Single();

        // Assert: the antes are what the ledger posted, and the support check reads their shape
        hand.Antes.Count(ante => ante != 0).Should().Be(posted);
        hand.Antes.Where(ante => ante != 0).Should().OnlyContain(ante => ante == 25m);
        PhhSupport.IsSupported(hand, out var reason).Should().BeTrue(reason);
        PhhSupport.AnteFor(hand, out var nominal, out var type, out _).Should().BeTrue();
        nominal.Should().Be(25m);
        type.Should().Be(anteType);
        hand.Players.Zip(hand.FinishingStacks!).ToDictionary(p => p.First, p => p.Second).Should().Equal(expectedStacks);

        // Act: replay it in a fresh session
        var (session, shuffler) = CreateSession();
        var controller = new PhhReplayController(session, shuffler);
        (await controller.LoadHandAsync(hand)).Should().BeTrue(controller.ReplayError);
        session.CurrentGame!.Options.Ante.Should().Be(25m);
        session.CurrentGame.Options.AnteType.Should().Be(anteType);
        while (!controller.IsComplete)
        {
            (await controller.ReplayNextActionAsync()).Should().BeTrue(controller.ReplayError);
        }

        // Assert
        FinalStacks(session.CurrentGame!).Should().Equal(expectedStacks);
        controller.FinalStacksMatch.Should().BeTrue(controller.FinalStacksMessage);
    }

    [TestMethod]
    public async Task Replay_WsopHandWithABigBlindAnte_EndsAtItsFinishingStacks()
    {
        // Arrange
        var hand = PhhReader.ReadAll(WsopHand).Single();
        PhhSupport.IsSupported(hand, out var reason).Should().BeTrue(reason);
        var (session, shuffler) = CreateSession();
        var controller = new PhhReplayController(session, shuffler);

        // Act
        (await controller.LoadHandAsync(hand)).Should().BeTrue(controller.ReplayError);
        while (!controller.IsComplete)
        {
            (await controller.ReplayNextActionAsync()).Should().BeTrue(controller.ReplayError);
        }

        // Assert
        session.CurrentGame!.Options.AnteType.Should().Be(AnteType.BigBlind);
        session.CurrentGame.Options.Ante.Should().Be(120000m);
        controller.FinalStacksMatch.Should().BeTrue(controller.FinalStacksMessage);
        session.CurrentGame.HandData!.Seats.OrderBy(seat => seat.SeatNo).Select(seat => seat.StartHandStack + seat.HandResult!.Net)
            .Should().Equal(hand.FinishingStacks!);
    }

    [TestMethod]
    public void IsSupported_AntesOfDifferentSizes_NamesTheReason()
    {
        var hand = PhhReader.ReadAll(WsopHand.Replace("antes = [0, 120000, 0, 0, 0]", "antes = [10, 20, 10, 10, 10]", StringComparison.Ordinal)).Single();

        PhhSupport.IsSupported(hand, out var reason).Should().BeFalse();
        reason.Should().Contain("antes");
    }
}
