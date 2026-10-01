using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.PostgreSql;
using OpenHoldemEngine.Sqlite;
using OpenHoldemEngine.SqlServer;
using OpenHoldemEngine.Tests.Persistence.Upgrades;

namespace OpenHoldemEngine.Tests.Persistence;

/// <summary>
/// The provider matrix: the same schema, upgrade and model checks on every database kind. SQLite and
/// InMemory run; PostgreSQL and SQL Server report inconclusive until the Testcontainers issue wires them in.
/// </summary>
[TestClass]
public sealed class ProviderSchemaTests
{
    private static readonly GameOptionData _options = new() { MaxPlayersCount = 2, InitialSmallBlind = 50M, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _joinSeats = [new("Button", 1, 10_000M), new("BigBlind", 2, 10_000M)];

    [TestMethod]
    [DataRow(TestDatabaseKind.Sqlite)]
    [DataRow(TestDatabaseKind.InMemory)]
    [DataRow(TestDatabaseKind.PostgreSql)]
    [DataRow(TestDatabaseKind.SqlServer)]
    public async Task MigratedSchema_MatchesModel(TestDatabaseKind database)
    {
        var configure = TestDatabase.Configure(database, out var lifetime);
        using var databaseLifetime = lifetime;
        using var provider = BuildProvider(configure);
        var manager = provider.GetRequiredService<IHoldemSchemaManager>();
        await manager.MigrateAsync();
        var status = await manager.GetSchemaStatusAsync();

        if (!TestDatabase.IsRelational(database))
        {
            status.State.Should().Be(HoldemSchemaState.NotVersioned);
            return;
        }

        await using var context = await provider.GetRequiredService<IDbContextFactory<HoldemDbContext>>().CreateDbContextAsync();
        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        context.Database.HasPendingModelChanges().Should().BeFalse("the migrations' snapshot must match HoldemDbContext's model");
        status.State.Should().Be(HoldemSchemaState.UpToDate);
        status.DatabaseSchemaVersion.Should().Be(HoldemSchema.Version);
    }

    [TestMethod]
    [DataRow(TestDatabaseKind.Sqlite)]
    [DataRow(TestDatabaseKind.InMemory)]
    [DataRow(TestDatabaseKind.PostgreSql)]
    [DataRow(TestDatabaseKind.SqlServer)]
    public async Task Upgrade_RerunningMigrateKeepsTheData(TestDatabaseKind database)
    {
        var configure = TestDatabase.Configure(database, out var lifetime);
        using var databaseLifetime = lifetime;
        var gameId = Guid.NewGuid();

        GameData before;
        using (var first = BuildProvider(configure))
        {
            await first.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
            var engine = first.GetRequiredService<HoldemEngine>();
            await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
            await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));
            await ApplyAsync(engine, new HoldemActionPlay(gameId, PlayerActionType.Fold, 0));
            before = await ApplyAsync(engine, new HoldemActionGetGame(gameId));
        }

        // A new container: its engine has its own lazy schema check, which must pass on the migrated database.
        using var second = BuildProvider(configure);
        await second.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        var after = await ApplyAsync(second.GetRequiredService<HoldemEngine>(), new HoldemActionGetGame(gameId));

        after.ShouldAsJToken().BeEquivalentToAsJToken(before);
    }

    [TestMethod]
    [DataRow(TestDatabaseKind.Sqlite)]
    [DataRow(TestDatabaseKind.PostgreSql)]
    [DataRow(TestDatabaseKind.SqlServer)]
    public async Task Upgrade_EveryMigrationKeepsItsSeededData(TestDatabaseKind database)
    {
        var migrationIds = MigrationIds(database);
        var fixtures = DiscoverFixtures();

        // Only InitialCreate exists today, so this iterates nothing; it starts testing with the second migration.
        for (var index = 1; index < migrationIds.Count; index++)
        {
            var fixture = fixtures.Single(candidate => migrationIds[index].EndsWith("_" + candidate.MigrationName, StringComparison.Ordinal));
            var configure = TestDatabase.Configure(database, out var lifetime);
            using var databaseLifetime = lifetime;
            using var provider = BuildProvider(configure);

            await using (var previous = await provider.GetRequiredService<IDbContextFactory<HoldemDbContext>>().CreateDbContextAsync())
            {
                await previous.GetService<IMigrator>().MigrateAsync(migrationIds[index - 1]);
                await fixture.SeedAsync(previous, database);
            }

            await provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
            await fixture.AssertAsync(provider, database);
        }
    }

    [TestMethod]
    public void Upgrade_EveryMigrationAfterTheFirstHasAFixture()
    {
        var fixtures = DiscoverFixtures();
        var problems = new List<string>();

        // With only InitialCreate today every provider's list is one long and the guard passes trivially.
        foreach (var provider in new[] { TestDatabaseKind.Sqlite, TestDatabaseKind.PostgreSql, TestDatabaseKind.SqlServer })
        {
            foreach (var id in MigrationIds(provider).Skip(1))
            {
                var matches = fixtures.Count(fixture => id.EndsWith("_" + fixture.MigrationName, StringComparison.Ordinal));
                if (matches != 1)
                {
                    problems.Add($"{provider}: migration {id} has {matches} upgrade fixtures, expected exactly 1");
                }
            }
        }

        problems.Should().BeEmpty("every migration after the first needs one IUpgradeFixture in Persistence/Upgrades");
    }

    /// <summary>The ordered migration ids of a provider package, read without opening a connection.</summary>
    private static List<string> MigrationIds(TestDatabaseKind kind)
    {
        var builder = new DbContextOptionsBuilder<HoldemDbContext>();
        switch (kind)
        {
            case TestDatabaseKind.Sqlite:
                builder.UseHoldemSqlite(":memory:");
                break;
            case TestDatabaseKind.PostgreSql:
                builder.UseHoldemPostgreSql("Host=localhost;Database=unused");
                break;
            case TestDatabaseKind.SqlServer:
                builder.UseHoldemSqlServer("Server=localhost;Database=unused");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only relational kinds have migrations.");
        }

        using var context = new HoldemDbContext(builder.Options);
        return context.Database.GetMigrations().ToList();
    }

    private static List<IUpgradeFixture> DiscoverFixtures()
        => typeof(ProviderSchemaTests).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IUpgradeFixture).IsAssignableFrom(type))
            .Select(type => (IUpgradeFixture)Activator.CreateInstance(type)!)
            .ToList();

    private static ServiceProvider BuildProvider(Action<DbContextOptionsBuilder> configure)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddHoldemEngine(dbOptionsAction: configure);
        return services.BuildServiceProvider();
    }

    private static async Task<GameData> ApplyAsync(HoldemEngine engine, HoldemAction action)
    {
        var result = await engine.ApplyHoldemActionAsync(action);
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
        return result.Value;
    }
}
