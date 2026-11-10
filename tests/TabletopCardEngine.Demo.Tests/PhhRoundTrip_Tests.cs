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
public sealed class PhhRoundTrip_Tests
{
    private static IHoldemTable CreateTestEngine()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(
            dbOptionsAction: opts => opts.UseHoldemSqlite(connection),
            configure: _ => { }
        ));

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();

        return serviceProvider.GetRequiredService<IHoldemTable>();
    }

    private static async Task<DemoGameSession> PlayAsync(GameOptionData options, PlayerJoinData[] joinSeats, Func<DemoGameSession, Task> play)
    {
        var session = new DemoGameSession(CreateTestEngine());
        (await session.CreateGameAsync(options, Guid.NewGuid())).Should().BeTrue();
        (await session.StartFirstHandAsync(joinSeats)).Should().BeTrue();
        await play(session);
        return session;
    }

    private static GameOptionData Options(byte seats, HoldemTableFormat format)
        => new() { TableFormat = format, MaxPlayersCount = seats, InitialSmallBlind = 50m, InitialDealerSeatNo = seats };

    private static PlayerJoinData[] Players(params decimal[] stacks)
        => [.. stacks.Select((stack, i) => new PlayerJoinData(SeatNo: (byte)(i + 1), Name: $"Player{i + 1}", BuyInAmount: stack))];

    /// <summary>Checks if it can, calls otherwise, until the hand is over.</summary>
    private static async Task CheckDownAsync(DemoGameSession session)
    {
        while (session.CurrentHand!.NextPlayerSeatNo != 0)
        {
            var canCheck = session.CurrentHand.Evaluations.Any(e => e.PlayerActionType == PlayerActionType.Check);
            (await session.ApplyPlayerActionAsync(canCheck ? PlayerActionType.Check : PlayerActionType.Call)).Should().BeTrue();
        }
    }

    private static Dictionary<string, decimal> FinalStacks(HandData hand)
        => hand.Seats.Where(seat => seat.HandResult != null)
            .ToDictionary(seat => seat.Name, seat => seat.StartHandStack + seat.HandResult!.Net);

    /// <summary>Exports the played hand, reads it back, replays it and returns the replayed hand's stacks.</summary>
    private static async Task<Dictionary<string, decimal>> ExportAndReplayAsync(DemoGameSession played)
    {
        var phh = PhhWriter.WriteHand(played.CurrentGame!, played.Journal);

        var hand = PhhReader.ReadAll(phh).Should().ContainSingle().Subject;
        PhhSupport.IsSupported(hand, out var reason).Should().BeTrue(reason);

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(opts => opts.UseHoldemSqlite(connection), _ => { }));
        var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();

        var shuffler = new PhhCardShuffler();
        var session = new DemoGameSession(provider.GetRequiredService<IHoldemTable>(), DemoReplayEngine.Create(connection, shuffler));
        var controller = new PhhReplayController(session, shuffler);

        (await controller.LoadHandAsync(hand)).Should().BeTrue(controller.ReplayError);
        while (!controller.IsComplete)
        {
            (await controller.ReplayNextActionAsync()).Should().BeTrue(controller.ReplayError);
        }

        controller.FinalStacksMatch.Should().BeTrue(controller.FinalStacksMessage);
        return FinalStacks(session.CurrentHand!);
    }

    [TestMethod]
    public async Task WriteHand_HeadsUpFoldPreFlop_ReadsBackAndReplaysToTheSameStacks()
    {
        var played = await PlayAsync(Options(2, HoldemTableFormat.HeadsUp), Players(1000m, 1000m), async session =>
            (await session.ApplyPlayerActionAsync(PlayerActionType.Fold)).Should().BeTrue());

        var phh = PhhWriter.WriteHand(played.CurrentGame!, played.Journal);
        phh.Should().StartWith("[1]\n").And.Contain("variant = 'NT'").And.NotContain("[Game]");

        var replayed = await ExportAndReplayAsync(played);

        replayed.Should().BeEquivalentTo(FinalStacks(played.CurrentHand!));
    }

    [TestMethod]
    public async Task WriteHand_SixHandedRaiseAndReRaiseToShowdown_ReadsBackAndReplaysToTheSameStacks()
    {
        var played = await PlayAsync(Options(6, HoldemTableFormat.SixMax), Players(10000m, 10000m, 10000m, 10000m, 10000m, 10000m), async session =>
        {
            var raises = 0;
            byte firstRaiser = 0;
            while (session.CurrentHand!.NextPlayerSeatNo != 0)
            {
                var hand = session.CurrentHand;
                if (hand.Street != Street.PreFlop)
                {
                    await CheckDownAsync(session);
                    break;
                }

                if (raises < 2)
                {
                    firstRaiser = raises == 0 ? hand.NextPlayerSeatNo : firstRaiser;
                    (await session.ApplyPlayerActionAsync(PlayerActionType.Raise, raises == 0 ? 300m : 900m)).Should().BeTrue();
                    raises++;
                }
                else
                {
                    var type = hand.NextPlayerSeatNo == firstRaiser ? PlayerActionType.Call : PlayerActionType.Fold;
                    (await session.ApplyPlayerActionAsync(type)).Should().BeTrue();
                }
            }
        });

        played.CurrentHand!.Actions.Select(a => a.Street).Distinct().Should().Contain([Street.Flop, Street.Turn, Street.River]);
        played.CurrentHand.Seats.Count(seat => !seat.IsFolded).Should().Be(2);

        var phh = PhhWriter.WriteHand(played.CurrentGame!, played.Journal);
        phh.Should().Contain(" sm ");

        var replayed = await ExportAndReplayAsync(played);

        replayed.Should().BeEquivalentTo(FinalStacks(played.CurrentHand));
    }

    [TestMethod]
    public async Task WriteHand_AllInAndRunOut_ReadsBackAndReplaysToTheSameStacks()
    {
        var played = await PlayAsync(Options(2, HoldemTableFormat.HeadsUp), Players(1000m, 400m), async session =>
        {
            (await session.ApplyPlayerActionAsync(PlayerActionType.AllIn)).Should().BeTrue();
            (await session.ApplyPlayerActionAsync(PlayerActionType.Call)).Should().BeTrue();
        });

        played.CurrentHand!.CommunityCards.Should().HaveCount(5);

        var phh = PhhWriter.WriteHand(played.CurrentGame!, played.Journal);
        phh.Should().Contain("d db ");

        var replayed = await ExportAndReplayAsync(played);

        replayed.Should().BeEquivalentTo(FinalStacks(played.CurrentHand));
    }

    [TestMethod]
    public async Task WriteSession_TwoHands_WritesOneNumberedTablePerHand()
    {
        var played = await PlayAsync(Options(2, HoldemTableFormat.HeadsUp), Players(1000m, 1000m), async session =>
        {
            (await session.ApplyPlayerActionAsync(PlayerActionType.Fold)).Should().BeTrue();
            (await session.StartNextHandAsync()).Should().BeTrue();
            (await session.ApplyPlayerActionAsync(PlayerActionType.Fold)).Should().BeTrue();
        });

        var phh = PhhWriter.WriteSession(played.CurrentGame!, played.Journal);

        var hands = PhhReader.ReadAll(phh);
        hands.Select(hand => hand.Index).Should().Equal(1, 2);
        hands.Should().OnlyContain(hand => hand.FinishingStacks != null);
        hands[1].StartingStacks.Should().NotEqual(hands[0].StartingStacks);
    }

    [TestMethod]
    public void WriteHand_WithoutDealtHand_ThrowsInvalidOperationException()
    {
        // Arrange
        var engine = CreateTestEngine();
        var session = new DemoGameSession(engine);
        var gameId = Guid.NewGuid();
        var options = new GameOptionData { TableFormat = HoldemTableFormat.HeadsUp, MaxPlayersCount = 2, InitialSmallBlind = 50m, InitialDealerSeatNo = 1 };

        // Create a GameData with no hand (null HandData)
        var gameData = new GameData(
            GameId: gameId,
            Options: options,
            HandData: null!
        );

        // Act & Assert - Game created but no hand dealt
        try
        {
            PhhWriter.WriteHand(gameData, session.Journal);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

}

