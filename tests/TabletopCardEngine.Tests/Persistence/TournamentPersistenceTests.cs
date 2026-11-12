using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Tests.Persistence;

/// <summary>The tournament tables of schema 7 through a <see cref="HoldemDbContext"/>, on the two providers that always run.</summary>
[TestClass]
public sealed class TournamentPersistenceTests
{
    [TestMethod]
    [DataRow(TestDatabaseKind.Sqlite)]
    [DataRow(TestDatabaseKind.InMemory)]
    public async Task Tournament_WithEntriesAndATable_ReadsBack(TestDatabaseKind database)
    {
        var configure = TestDatabase.Configure(database, out var lifetime);
        using var databaseLifetime = lifetime;
        using var provider = await BuildProviderAsync(configure);
        var gameId = await CreateGameAsync(provider);
        var tournamentId = Guid.NewGuid();
        var factory = provider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Add(new Tournament
            {
                Id = tournamentId,
                BettingStructure = BettingStructure.PotLimit,
                TableFormat = HoldemTableFormat.FullRing,
                MaxPlayersPerTable = 9,
                RaiseCap = null,
                AnteType = AnteType.BigBlind,
                StartingStack = 1_500M,
                BuyIn = 10.5M,
                MinEntries = 2,
                MaxEntries = 90,
                Levels = [new TournamentLevel(25M, 0M, 600), new TournamentLevel(50M, 5M, 600)],
                Payouts = [12.25M, 5M],
                State = 2,
                SeatDrawSeed = 42,
                StartedAt = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero),
                Version = 1,
                Entries =
                [
                    new TournamentEntry { EntryNo = 1, Name = "Alice", TableNo = 1, SeatNo = 3 },
                    new TournamentEntry { EntryNo = 2, Name = "Bob", CarriedStack = 700.5M, Place = 2, Prize = 5M, EliminatedInHandNo = 4, EliminatedAtTableNo = 1 },
                ],
                Tables = [new TournamentTable { TableNo = 1, GameId = gameId }],
            });
            await context.SaveChangesAsync();
        }

        await using var check = await factory.CreateDbContextAsync();
        var read = await check.Set<Tournament>().Include(tournament => tournament.Entries).Include(tournament => tournament.Tables)
            .SingleAsync(tournament => tournament.Id == tournamentId);

        read.BettingStructure.Should().Be(BettingStructure.PotLimit);
        read.TableFormat.Should().Be(HoldemTableFormat.FullRing);
        read.AnteType.Should().Be(AnteType.BigBlind);
        read.MaxPlayersPerTable.Should().Be(9);
        read.RaiseCap.Should().BeNull();
        read.StartingStack.Should().Be(1_500M);
        read.BuyIn.Should().Be(10.5M);
        read.MaxEntries.Should().Be(90);
        read.Levels.Should().Equal([new TournamentLevel(25M, 0M, 600), new TournamentLevel(50M, 5M, 600)]);
        read.Payouts.Should().Equal(12.25M, 5M);
        read.State.Should().Be(2);
        read.SeatDrawSeed.Should().Be(42);
        read.StartedAt.Should().Be(new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero));
        read.FinishedAt.Should().BeNull();
        read.Entries.OrderBy(entry => entry.EntryNo).Select(entry => (entry.Name, entry.TableNo, entry.SeatNo, entry.CarriedStack, entry.Place, entry.Prize, entry.EliminatedInHandNo, entry.EliminatedAtTableNo))
            .Should().Equal(
                ("Alice", 1, (byte?)3, (decimal?)null, (int?)null, 0M, (int?)null, (int?)null),
                ("Bob", null, null, 700.5M, 2, 5M, 4, 1));
        read.Tables.Should().ContainSingle().Which.GameId.Should().Be(gameId);
        read.Tables[0].IsBroken.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(TestDatabaseKind.Sqlite)]
    [DataRow(TestDatabaseKind.InMemory)]
    public async Task Tournament_StaleVersion_ThrowsOnSave(TestDatabaseKind database)
    {
        var configure = TestDatabase.Configure(database, out var lifetime);
        using var databaseLifetime = lifetime;
        using var provider = await BuildProviderAsync(configure);
        var tournamentId = Guid.NewGuid();
        var factory = provider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();

        await using (var seed = await factory.CreateDbContextAsync())
        {
            seed.Add(new Tournament { Id = tournamentId, Version = 1 });
            await seed.SaveChangesAsync();
        }

        await using var first = await factory.CreateDbContextAsync();
        await using var second = await factory.CreateDbContextAsync();
        var firstCopy = await first.Set<Tournament>().SingleAsync(tournament => tournament.Id == tournamentId);
        var secondCopy = await second.Set<Tournament>().SingleAsync(tournament => tournament.Id == tournamentId);

        firstCopy.State = 2;
        firstCopy.Version++;
        await first.SaveChangesAsync();

        secondCopy.State = 3;
        secondCopy.Version++;
        var act = () => second.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [TestMethod]
    public async Task TournamentTable_SecondTableOnTheSameGame_IsRejected()
    {
        var configure = TestDatabase.Configure(TestDatabaseKind.Sqlite, out var lifetime);
        using var databaseLifetime = lifetime;
        using var provider = await BuildProviderAsync(configure);
        var gameId = await CreateGameAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();

        await using var context = await factory.CreateDbContextAsync();
        context.Add(new Tournament
        {
            Id = Guid.NewGuid(),
            Version = 1,
            Tables = [new TournamentTable { TableNo = 1, GameId = gameId }, new TournamentTable { TableNo = 2, GameId = gameId }],
        });
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>("a game belongs to at most one tournament table");
    }

    [TestMethod]
    public async Task Sqlite_ListsTheThreeTournamentTables()
    {
        var configure = TestDatabase.Configure(TestDatabaseKind.Sqlite, out var lifetime);
        using var databaseLifetime = lifetime;
        using var provider = await BuildProviderAsync(configure);

        await using var context = await provider.GetRequiredService<IDbContextFactory<HoldemDbContext>>().CreateDbContextAsync();
        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT name AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name LIKE 'tce_holdem_Tournament%' ORDER BY name")
            .ToListAsync();

        tables.Should().Equal("tce_holdem_Tournament", "tce_holdem_TournamentEntry", "tce_holdem_TournamentTable");
    }

    private static async Task<ServiceProvider> BuildProviderAsync(Action<DbContextOptionsBuilder> configure)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(dbOptionsAction: configure));
        var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        return provider;
    }

    private static async Task<Guid> CreateGameAsync(IServiceProvider provider)
    {
        var gameId = Guid.NewGuid();
        var result = await provider.GetRequiredService<IHoldemTable>()
            .ApplyAsync(new HoldemActionCreateGame(gameId, new GameOptionData { MaxPlayersCount = 2, InitialSmallBlind = 50M, InitialDealerSeatNo = 1 }));
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
        return gameId;
    }
}
