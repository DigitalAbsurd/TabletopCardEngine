using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.Demo.Tests;

/// <summary>
/// The reset behind Apply on the New Game modal: the engine's rows go, the schema stays, and the
/// same connection keeps working afterwards — it is the in-memory database's only lifeline.
/// </summary>
[TestClass]
public sealed class DemoDatabase_Tests
{
    private static ServiceProvider BuildServices(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(
            dbOptionsAction: opts => opts.UseHoldemSqlite(connection),
            configure: _ => { }));
        services.AddSingleton(connection);
        services.AddScoped<SqliteBrowser>();
        services.AddScoped<DemoDatabase>();
        services.AddScoped<DemoGameSession>();

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();

        return serviceProvider;
    }

    private static async Task PlayOneHandAsync(DemoGameSession session)
    {
        await session.CreateGameAsync(DemoGameDefaults.CreateOptions());
        await session.StartFirstHandAsync(DemoGameDefaults.CreateJoinSeats());
    }

    [TestMethod]
    public async Task DemoDatabase_ClearAllData_EmptiesEveryTable()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var serviceProvider = BuildServices(connection);
        var session = serviceProvider.GetRequiredService<DemoGameSession>();
        var browser = serviceProvider.GetRequiredService<SqliteBrowser>();
        var database = serviceProvider.GetRequiredService<DemoDatabase>();

        await PlayOneHandAsync(session);
        browser.GetRowCount(HoldemSchema.SqliteTablePrefix + "Game").Should().Be(1);
        browser.GetRowCount(HoldemSchema.SqliteTablePrefix + "HandAction").Should().BeGreaterThan(0);

        string[] versionTables = [HoldemSchema.SqliteMigrationsHistoryTable, HoldemSchema.SqliteTablePrefix + HoldemSchema.SchemaTable];
        var versionRowCounts = versionTables.ToDictionary(tableName => tableName, browser.GetRowCount);
        versionRowCounts.Values.Should().OnlyContain(count => count > 0);

        // Act
        database.ClearAllData();

        // Assert
        foreach (var tableName in browser.GetTableNames())
        {
            if (versionTables.Contains(tableName))
            {
                browser.GetRowCount(tableName).Should().Be(versionRowCounts[tableName], $"{tableName} is a schema table and must not be cleared");
            }
            else if (tableName.StartsWith("__", StringComparison.Ordinal))
            {
                // Other bookkeeping (EF's migration lock) is never cleared, and holds no rows to begin with.
                continue;
            }
            else
            {
                browser.GetRowCount(tableName).Should().Be(0, $"{tableName} must be empty after a reset");
            }
        }
    }

    [TestMethod]
    public async Task DemoDatabase_ClearAllData_LeavesTheSchemaInPlace()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var serviceProvider = BuildServices(connection);
        var session = serviceProvider.GetRequiredService<DemoGameSession>();
        var browser = serviceProvider.GetRequiredService<SqliteBrowser>();
        var database = serviceProvider.GetRequiredService<DemoDatabase>();

        await PlayOneHandAsync(session);
        var tablesBefore = browser.GetTableNames();

        // Act
        database.ClearAllData();

        // Assert - the tables and the connection both survive, which dropping the database would not do
        browser.GetTableNames().Should().Equal(tablesBefore);
        connection.State.Should().Be(System.Data.ConnectionState.Open);
    }

    [TestMethod]
    public async Task DemoDatabase_ClearAllData_LetsTheEngineStartAGameOverTheOldOnesId()
    {
        // Arrange - the same game id is the strictest proof the rows really went
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var serviceProvider = BuildServices(connection);
        var session = serviceProvider.GetRequiredService<DemoGameSession>();
        var database = serviceProvider.GetRequiredService<DemoDatabase>();

        var gameId = Guid.NewGuid();
        await session.CreateGameAsync(DemoGameDefaults.CreateOptions(), gameId);
        await session.StartFirstHandAsync(DemoGameDefaults.CreateJoinSeats());

        // Act
        database.ClearAllData();
        session.Reset();
        var recreated = await session.CreateGameAsync(new GameOptionData { MaxPlayersCount = 2 }, gameId);

        // Assert
        recreated.Should().BeTrue("the row that made this id taken has been deleted");
        session.CurrentGame!.Options.MaxPlayersCount.Should().Be(2);
    }

    [TestMethod]
    public async Task DemoDatabase_ClearAllData_KeepsASecondEngineRunnable()
    {
        // Arrange - two engines over the same connection
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        // First engine
        using var serviceProvider1 = BuildServices(connection);
        var session1 = serviceProvider1.GetRequiredService<DemoGameSession>();
        await PlayOneHandAsync(session1);

        var database = serviceProvider1.GetRequiredService<DemoDatabase>();

        // Act - clear and build a second engine
        database.ClearAllData();

        // Second engine over the same connection should work fine
        using var serviceProvider2 = BuildServices(connection);
        var session2 = serviceProvider2.GetRequiredService<DemoGameSession>();

        // Assert - the second engine can create a game without throwing
        var gameId = Guid.NewGuid();
        var result = await session2.CreateGameAsync(DemoGameDefaults.CreateOptions(), gameId);
        result.Should().BeTrue("the second engine should be able to create a game after ClearAllData");
    }
}
