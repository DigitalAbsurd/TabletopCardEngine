using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Tournaments;
using TabletopCardEngine.InMemory;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The host loop of <c>docs/concepts/tournaments.md</c> and the short example of the READMEs, copied here line for
/// line and run. Change one and the other must follow.
/// </summary>
[TestClass]
public sealed class TournamentDocsExampleTests
{
    [TestMethod]
    public async Task ReadmeExample_CreatesRegistersStartsAndDealsATable()
    {
        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemInMemory(Guid.NewGuid().ToString())));
        using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        var table = provider.GetRequiredService<IHoldemTable>();

        var tournaments = provider.GetRequiredService<IHoldemTournament>();

        var created = await tournaments.ApplyAsync(TournamentAction.CreateTournament(new TournamentOptionData
        {
            StartingStack = 1000,
            BuyIn = 10,
            Payouts = [65, 35],
            Levels = [new TournamentLevelData { SmallBlind = 25, Duration = TimeSpan.FromMinutes(10) }],
        }));
        var tournamentId = created.Value!.TournamentId;

        await tournaments.ApplyAsync(TournamentAction.Register(tournamentId, "Alice"));
        await tournaments.ApplyAsync(TournamentAction.Register(tournamentId, "Bob"));

        // Closes registration, seats the players and deals the first hand of every table.
        var data = (await tournaments.ApplyAsync(TournamentAction.StartTournament(tournamentId))).Value!;
        data.State.Should().Be(TournamentState.Running);

        // The snippet's last line deals once a hand has ended, so end the hand first: the player on the clock folds.
        var gameId = data.Tables[0].GameId;
        (await table.ApplyAsync(HoldemAction.Play(gameId, PlayerActionType.Fold))).IsSuccess.Should().BeTrue();

        data = (await tournaments.ApplyAsync(TournamentAction.DealTable(tournamentId, tableNo: 1))).Value!;
        data.State.Should().Be(TournamentState.Running);
    }

    [TestMethod]
    public async Task TournamentExample_PlaysASitAndGoToTheEnd()
    {
        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(db => db.UseHoldemInMemory(Guid.NewGuid().ToString())));
        using var provider = services.BuildServiceProvider();

        // Create or upgrade the schema once, at start-up.
        await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        var table = provider.GetRequiredService<IHoldemTable>();
        var tournaments = provider.GetRequiredService<IHoldemTournament>();

        // A sit-and-go: three players with 1000 each, a buy-in of 10, the top two places paid 65/35,
        // and three blind levels of ten minutes, the second and third with an ante.
        var created = await tournaments.ApplyAsync(TournamentAction.CreateTournament(new TournamentOptionData
        {
            StartingStack = 1000,
            BuyIn = 10,
            Payouts = [65, 35],
            Levels =
            [
                new TournamentLevelData { SmallBlind = 25, Duration = TimeSpan.FromMinutes(10) },
                new TournamentLevelData { SmallBlind = 50, Ante = 5, Duration = TimeSpan.FromMinutes(10) },
                new TournamentLevelData { SmallBlind = 100, Ante = 10, Duration = TimeSpan.FromMinutes(10) },
            ],
        }));
        var tournamentId = created.Value!.TournamentId;

        foreach (var name in new[] { "Alice", "Bob", "Carol" })
        {
            await tournaments.ApplyAsync(TournamentAction.Register(tournamentId, name));
        }

        // StartTournament closes registration, draws the seats and deals the first hand of every table.
        var data = (await tournaments.ApplyAsync(TournamentAction.StartTournament(tournamentId))).Value!;

        while (data.State == TournamentState.Running)
        {
            foreach (var tournamentTable in data.Tables.Where(t => !t.IsBroken))
            {
                // Play the hand on the table's game, as for a cash game: shove when raising is open, otherwise
                // check or call.
                var game = (await table.ApplyAsync(HoldemAction.GetGame(tournamentTable.GameId))).Value!;
                while (game.HandData!.NextPlayerSeatNo != 0)
                {
                    var offered = game.HandData.Evaluations.Select(e => e.PlayerActionType).ToList();
                    var action = offered.Contains(PlayerActionType.Raise) ? PlayerActionType.AllIn
                        : offered.Contains(PlayerActionType.Check) ? PlayerActionType.Check
                        : PlayerActionType.Call;
                    var played = await table.ApplyAsync(HoldemAction.Play(tournamentTable.GameId, action));
                    if (!played.IsSuccess)
                    {
                        throw new InvalidOperationException(played.Errors[0].Message);
                    }

                    game = played.Value!;
                }

                // Nothing deals the next hand by itself: DealTable records the busts and deals it, or ends the tournament.
                var dealt = await tournaments.ApplyAsync(TournamentAction.DealTable(tournamentId, tournamentTable.TableNo));
                if (!dealt.IsSuccess)
                {
                    throw new InvalidOperationException(dealt.Errors[0].Message);
                }

                data = dealt.Value!;
            }
        }

        foreach (var entry in data.Entries.OrderBy(e => e.Place))
        {
            Console.WriteLine($"{entry.Place}. {entry.Name}: {entry.Prize}");
        }

        data.State.Should().Be(TournamentState.Finished);

        // Players busted in one hand with equal stacks tie, so the places are 1, 2, 3 or 1, 2, 2.
        data.Entries.Should().OnlyContain(e => e.Place != null);
        data.Entries.Count(e => e.Place == 1).Should().Be(1);
        data.Entries.Sum(e => e.Prize).Should().Be(data.PrizePool).And.Be(30);
        data.Entries.Single(e => e.Place == 1).State.Should().Be(TournamentEntryState.Winner);
    }
}
