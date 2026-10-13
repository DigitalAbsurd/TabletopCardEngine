using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The <c>TabletopCardEngine.Sqlite</c> package: its migrations create the schema through
/// <see cref="IHoldemSchemaManager.MigrateAsync"/>, agree with the model, keep their history in the
/// engine's own table, and bring no other database driver along.
/// </summary>
[TestClass]
public sealed class SqlitePackageTests
{
    private static readonly GameOptionData _options = new() { MaxPlayersCount = 2, InitialSmallBlind = 50M, InitialDealerSeatNo = 1 };

    private static readonly PlayerJoinData[] _joinSeats = [new("Button", 1, 10_000M), new("BigBlind", 2, 10_000M)];

    private static readonly string[] _otherDrivers =
    [
        "Npgsql",
        "Microsoft.Data.SqlClient",
        "Microsoft.EntityFrameworkCore.SqlServer",
        "Microsoft.EntityFrameworkCore.InMemory",
    ];

    [TestMethod]
    public async Task Sqlite_MigrateAsync_CreatesAnUpToDateSchema()
    {
        using var host = MigratedHost.Create();
        var manager = host.GetRequiredService<IHoldemSchemaManager>();

        await manager.MigrateAsync();
        var status = await manager.GetSchemaStatusAsync();

        status.State.Should().Be(HoldemSchemaState.UpToDate);
        status.PendingMigrations.Should().BeEmpty();
        status.DatabaseSchemaVersion.Should().Be(1);

        var engine = host.GetRequiredService<IHoldemTable>();
        var gameId = Guid.NewGuid();
        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));
    }

    [TestMethod]
    public async Task Sqlite_MigrateAsync_IsIdempotent()
    {
        using var host = MigratedHost.Create();
        var manager = host.GetRequiredService<IHoldemSchemaManager>();
        await manager.MigrateAsync();

        var engine = host.GetRequiredService<IHoldemTable>();
        var gameId = Guid.NewGuid();
        await ApplyAsync(engine, new HoldemActionCreateGame(gameId, _options));
        var started = await ApplyAsync(engine, new HoldemActionStartGame(gameId, _joinSeats));
        var statusBefore = await manager.GetSchemaStatusAsync();
        var appliedBefore = await AppliedMigrationsAsync(host);

        await manager.MigrateAsync();

        (await manager.GetSchemaStatusAsync()).Should().Be(statusBefore);
        (await AppliedMigrationsAsync(host)).Should().Equal(appliedBefore);
        var reloaded = await ApplyAsync(engine, new HoldemActionGetGame(gameId));
        reloaded.ShouldAsJToken().BeEquivalentToAsJToken(started);
    }

    [TestMethod]
    public async Task Sqlite_MigratedSchema_HasNoPendingModelChanges()
    {
        using var host = MigratedHost.Create();
        var contextFactory = host.GetRequiredService<IDbContextFactory<HoldemDbContext>>();

        // Checked before migrating as well: EF refuses to migrate over a model that has drifted
        // from the snapshot, and this assertion says why more plainly than that exception does.
        await using (var unmigrated = await contextFactory.CreateDbContextAsync())
        {
            unmigrated.Database.HasPendingModelChanges().Should().BeFalse("the migrations' snapshot must match HoldemDbContext's model");
        }

        await host.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
        await using var context = await contextFactory.CreateDbContextAsync();

        context.Database.HasPendingModelChanges().Should().BeFalse("the migrations' snapshot must match HoldemDbContext's model");
    }

    [TestMethod]
    public async Task Sqlite_UsesTheEngineHistoryTable()
    {
        using var host = MigratedHost.Create();
        await host.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();

        await using var context = await host.GetRequiredService<IDbContextFactory<HoldemDbContext>>().CreateDbContextAsync();
        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToListAsync();

        tables.Should().Contain("__TceMigrationsHistory");
        tables.Should().NotContain("__EFMigrationsHistory");
    }

    [TestMethod]
    public void SqlitePackage_PullsNoOtherDriver()
    {
        var assetsFile = Path.Combine(RepositoryRoot.FullPath, "src", "TabletopCardEngine.Sqlite", "obj", "project.assets.json");
        File.Exists(assetsFile).Should().BeTrue("the package must be restored before the tests run");

        using var assets = JsonDocument.Parse(File.ReadAllText(assetsFile));
        var libraries = assets.RootElement.GetProperty("libraries").EnumerateObject().Select(library => library.Name).ToList();

        libraries.Should().Contain(library => library.StartsWith("Microsoft.EntityFrameworkCore.Sqlite/", StringComparison.OrdinalIgnoreCase));
        libraries.Where(library => _otherDrivers.Any(driver => library.StartsWith(driver, StringComparison.OrdinalIgnoreCase)))
            .Should().BeEmpty();
    }

    private static async Task<GameData> ApplyAsync(IHoldemTable engine, HoldemAction action)
    {
        var result = await engine.ApplyAsync(action);
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
        return result.Value;
    }

    private static async Task<List<string>> AppliedMigrationsAsync(MigratedHost host)
    {
        await using var context = await host.GetRequiredService<IDbContextFactory<HoldemDbContext>>().CreateDbContextAsync();
        return (await context.Database.GetAppliedMigrationsAsync()).ToList();
    }

    /// <summary>
    /// A container configured the way a consumer of the package configures it, over a kept-alive
    /// in-memory connection. Nothing creates the schema: that is left to the test's own
    /// <see cref="IHoldemSchemaManager.MigrateAsync"/>.
    /// </summary>
    private sealed class MigratedHost : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _serviceProvider;

        private MigratedHost(SqliteConnection connection, ServiceProvider serviceProvider)
        {
            _connection = connection;
            _serviceProvider = serviceProvider;
        }

        internal static MigratedHost Create()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            var services = new ServiceCollection();
            services.AddTabletopCardEngine(tce => tce.AddHoldem(o => o.UseHoldemSqlite(connection)));
            return new MigratedHost(connection, services.BuildServiceProvider());
        }

        internal T GetRequiredService<T>() where T : notnull
            => _serviceProvider.GetRequiredService<T>();

        public void Dispose()
        {
            _serviceProvider.Dispose();
            _connection.Dispose();
        }
    }
}
