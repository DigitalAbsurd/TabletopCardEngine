using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.InMemory;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The README quick start and <c>docs/getting-started.md</c> must stay in step with this test.
/// </summary>
[TestClass]
public sealed class QuickStartTests
{
    [TestMethod]
    public async Task QuickStart_PlaysAHeadsUpHandToTheEnd()
    {
        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemInMemory(Guid.NewGuid().ToString())));
        using var provider = services.BuildServiceProvider();

        // Create or upgrade the schema once, at start-up.
        await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        var table = provider.GetRequiredService<IHoldemTable>();

        // A heads-up game: blinds 1/2, two players with 100 each.
        var created = await table.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
        {
            TableFormat = HoldemTableFormat.HeadsUp,
            MaxPlayersCount = 2,
            InitialSmallBlind = 1,
        }));
        var gameId = created.Value!.GameId;

        // StartGame seats the players, deals hand 1 and posts the blinds.
        var game = (await table.ApplyAsync(HoldemAction.StartGame(gameId,
        [
            new PlayerJoinData("Alice", 1, 100),
            new PlayerJoinData("Bob", 2, 100),
        ]))).Value!;

        // Play the hand to the end: check when allowed, otherwise call.
        while (game.HandData!.NextPlayerSeatNo != 0)
        {
            var canCheck = game.HandData.Evaluations.Any(e => e.PlayerActionType == PlayerActionType.Check);
            var action = canCheck ? PlayerActionType.Check : PlayerActionType.Call;
            var result = await table.ApplyAsync(HoldemAction.Play(gameId, action));
            if (!result.IsSuccess)
            {
                Assert.Fail(result.Errors[0].Message);
            }

            game = result.Value!;
        }

        Console.WriteLine($"Board: {string.Join(" ", game.HandData.CommunityCards)}");
        foreach (var seat in game.HandData.Seats)
        {
            // Stack excludes the winnings until the next hand; the outcome is HandResult.Net.
            var net = seat.HandResult!.Net;
            Console.WriteLine($"{seat.Name}: {string.Join(" ", seat.Hole ?? [])}, {seat.HandResult.Description}, {net:+0;-0;0}");
        }

        game.HandData.Street.Should().Be(Street.Finished);
        game.HandData.Seats.Sum(s => s.HandResult!.Net).Should().Be(0);
    }

    [TestMethod]
    public async Task QuickStart_JoinsAndLeavesSeats()
    {
        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemInMemory(Guid.NewGuid().ToString())));
        using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        var table = provider.GetRequiredService<IHoldemTable>();

        // Create and start a 6-max game
        var created = await table.ApplyAsync(HoldemAction.CreateGame(new GameOptionData
        {
            TableFormat = HoldemTableFormat.SixMax,
            MaxPlayersCount = 6,
            InitialSmallBlind = 1,
        }));
        var gameId = created.Value!.GameId;

        // Start with two players
        var game = (await table.ApplyAsync(HoldemAction.StartGame(gameId,
        [
            new PlayerJoinData("Alice", 1, 100),
            new PlayerJoinData("Bob", 2, 100),
        ]))).Value!;

        // Play one hand to completion
        while (game.HandData!.NextPlayerSeatNo != 0)
        {
            var canCheck = game.HandData.Evaluations.Any(e => e.PlayerActionType == PlayerActionType.Check);
            var action = canCheck ? PlayerActionType.Check : PlayerActionType.Call;
            game = (await table.ApplyAsync(HoldemAction.Play(gameId, action))).Value!;
        }

        // Seat a third player joining between hands
        game = (await table.ApplyAsync(
            HoldemAction.StartHand(gameId,
            [
                new PlayerJoinData("Dana", 3, 100),
            ]))).Value!;
        game.HandData!.Seats.Should().HaveCount(3);

        // Play another hand to completion
        while (game.HandData!.NextPlayerSeatNo != 0)
        {
            var canCheck = game.HandData.Evaluations.Any(e => e.PlayerActionType == PlayerActionType.Check);
            var action = canCheck ? PlayerActionType.Check : PlayerActionType.Call;
            game = (await table.ApplyAsync(HoldemAction.Play(gameId, action))).Value!;
        }

        // Stand Alice up (she was in seat 1)
        var afterLeave = (await table.ApplyAsync(HoldemAction.LeaveSeat(gameId, 1))).Value!;
        afterLeave.Should().NotBeNull();
        // After LeaveSeat between hands, start the next hand to confirm Alice is no longer seated
        var nextHand = (await table.ApplyAsync(HoldemAction.StartHand(gameId))).Value!;
        nextHand.HandData!.Seats.Should().HaveCount(2);
        nextHand.HandData.Seats.Should().NotContain(s => s.Name == "Alice");
    }
}
